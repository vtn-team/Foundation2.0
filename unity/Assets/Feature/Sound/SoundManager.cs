using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using R3;

/// <summary>
/// 再生情報
/// </summary>
[Serializable]
public class PlaybackInfo
{
    public string PlayerId;           // SoundPlayerのUUID
    public int ChannelId;             // 割り当てられたチャンネルID
    public ChannelGroup Group;        // チャンネルグループ
    public string SoundKey;           // 再生キー
    public float StartTime;           // 再生開始時刻
    public int Priority;              // 優先度
    public AudioSource AudioSource;   // 使用中のAudioSource

    public bool IsPlaying => AudioSource != null && AudioSource.isPlaying;
}

/// <summary>
/// チャンネル設定
/// </summary>
[Serializable]
public class ChannelConfig
{
    public ChannelGroup Group;
    public int ChannelCount;
    public AudioMixerGroup MixerGroup;
}

/// <summary>
/// インゲーム用サウンドマネージャー
/// </summary>
public class SoundManager : MonoBehaviour
{
    private static SoundManager _instance;

    public static SoundManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[SoundManager]");
                _instance = go.AddComponent<SoundManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Sound Dictionary")]
    [SerializeField]
    private SoundDictionary[] soundDictionaries;

    [Header("Audio Mixer")]
    [SerializeField]
    private AudioMixer audioMixer;

    [Header("Channel Configuration")]
    [SerializeField]
    private List<ChannelConfig> channelConfigs = new List<ChannelConfig>
    {
        new ChannelConfig { Group = ChannelGroup.SE_A, ChannelCount = 3 },
        new ChannelConfig { Group = ChannelGroup.SE_B, ChannelCount = 3 },
        new ChannelConfig { Group = ChannelGroup.Jingle, ChannelCount = 1 },
        new ChannelConfig { Group = ChannelGroup.Voice, ChannelCount = 1 }
    };

    // グループごとのAudioSourceプール
    private Dictionary<ChannelGroup, List<AudioSource>> _audioSourcePools;

    // SoundDictionaryのマージ済みデータ
    private Dictionary<string, SoundDictionaryItem> _soundDataMap;

    // 再生中のプレイヤー情報
    private Dictionary<string, PlaybackInfo> _activePlaybacks;

    // チャンネルIDカウンター
    private int _channelIdCounter;

    // 初期化済みフラグ
    private bool _isInitialized;

    // Dispose用
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void OnDestroy()
    {
        Dispose();
    }

    /// <summary>
    /// 初期化
    /// </summary>
    private void Initialize()
    {
        if (_isInitialized) return;

        _audioSourcePools = new Dictionary<ChannelGroup, List<AudioSource>>();
        _soundDataMap = new Dictionary<string, SoundDictionaryItem>();
        _activePlaybacks = new Dictionary<string, PlaybackInfo>();
        _channelIdCounter = 0;

        InitializeAudioSourcePools();
        LoadSoundDictionaries();

        _isInitialized = true;
    }

    /// <summary>
    /// AudioSourceプールの初期化
    /// </summary>
    private void InitializeAudioSourcePools()
    {
        foreach (var config in channelConfigs)
        {
            var pool = new List<AudioSource>();

            for (int i = 0; i < config.ChannelCount; i++)
            {
                var go = new GameObject($"AudioSource_{config.Group}_{i}");
                go.transform.SetParent(transform);

                var audioSource = go.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;

                if (config.MixerGroup != null)
                {
                    audioSource.outputAudioMixerGroup = config.MixerGroup;
                }

                pool.Add(audioSource);
            }

            _audioSourcePools[config.Group] = pool;
        }
    }

    /// <summary>
    /// SoundDictionaryの読み込み
    /// </summary>
    private void LoadSoundDictionaries()
    {
        _soundDataMap.Clear();

        if (soundDictionaries == null) return;

        foreach (var dictionary in soundDictionaries)
        {
            if (dictionary == null) continue;

            foreach (var item in dictionary.Items)
            {
                if (string.IsNullOrEmpty(item.Key)) continue;

                if (_soundDataMap.ContainsKey(item.Key))
                {
                    Debug.LogWarning($"[SoundManager] 重複キー検出: {item.Key}");
                    continue;
                }

                _soundDataMap[item.Key] = item;
            }
        }
    }

    /// <summary>
    /// SEを再生（簡易版）
    /// </summary>
    public int PlaySE(string soundKey, float volume = 1.0f)
    {
        return RequestPlay(System.Guid.NewGuid().ToString(), soundKey, volume, false);
    }

    /// <summary>
    /// Voiceを再生（簡易版）
    /// </summary>
    public int PlayVoice(string soundKey, float volume = 1.0f)
    {
        return RequestPlay(System.Guid.NewGuid().ToString(), soundKey, volume, false);
    }

    /// <summary>
    /// 再生リクエスト
    /// </summary>
    public int RequestPlay(string playerId, string soundKey, float volume = 1.0f, bool loop = false)
    {
        if (!_soundDataMap.TryGetValue(soundKey, out var soundItem))
        {
            Debug.LogWarning($"[SoundManager] サウンドキーが見つかりません: {soundKey}");
            return -1;
        }

        var group = GetChannelGroupForSoundType(soundItem.SoundType);

        var audioSource = GetAvailableAudioSource(group, soundItem.Priority);
        if (audioSource == null)
        {
            Debug.LogWarning($"[SoundManager] 利用可能なチャンネルがありません: {group}");
            return -1;
        }

        var channelId = ++_channelIdCounter;
        var playbackInfo = new PlaybackInfo
        {
            PlayerId = playerId,
            ChannelId = channelId,
            Group = group,
            SoundKey = soundKey,
            StartTime = Time.time,
            Priority = soundItem.Priority,
            AudioSource = audioSource
        };

        if (_activePlaybacks.ContainsKey(playerId))
        {
            StopPlayback(playerId);
        }

        _activePlaybacks[playerId] = playbackInfo;

        audioSource.clip = soundItem.AudioClip;
        audioSource.volume = volume;
        audioSource.loop = loop;
        audioSource.Play();

        return channelId;
    }

    /// <summary>
    /// 停止リクエスト
    /// </summary>
    public void RequestStop(string playerId)
    {
        StopPlayback(playerId);
    }

    /// <summary>
    /// 一時停止リクエスト
    /// </summary>
    public void RequestPause(string playerId)
    {
        if (_activePlaybacks.TryGetValue(playerId, out var info))
        {
            info.AudioSource?.Pause();
        }
    }

    /// <summary>
    /// 再開リクエスト
    /// </summary>
    public void RequestResume(string playerId)
    {
        if (_activePlaybacks.TryGetValue(playerId, out var info))
        {
            info.AudioSource?.UnPause();
        }
    }

    /// <summary>
    /// 再生状態を確認
    /// </summary>
    public bool IsPlaying(string playerId)
    {
        if (_activePlaybacks.TryGetValue(playerId, out var info))
        {
            return info.IsPlaying;
        }
        return false;
    }

    /// <summary>
    /// 再生情報を取得
    /// </summary>
    public PlaybackInfo GetPlaybackInfo(string playerId)
    {
        _activePlaybacks.TryGetValue(playerId, out var info);
        return info;
    }

    /// <summary>
    /// サウンドデータを取得
    /// </summary>
    public SoundDictionaryItem GetSoundData(string soundKey)
    {
        _soundDataMap.TryGetValue(soundKey, out var item);
        return item;
    }

    /// <summary>
    /// マスター音量を設定
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        if (audioMixer != null)
        {
            float db = volume > 0 ? Mathf.Log10(volume) * 20f : -80f;
            audioMixer.SetFloat("MasterVolume", db);
        }
    }

    /// <summary>
    /// グループ音量を設定
    /// </summary>
    public void SetGroupVolume(ChannelGroup group, float volume)
    {
        if (audioMixer != null)
        {
            string paramName = $"{group}Volume";
            float db = volume > 0 ? Mathf.Log10(volume) * 20f : -80f;
            audioMixer.SetFloat(paramName, db);
        }
    }

    private ChannelGroup GetChannelGroupForSoundType(SoundType soundType)
    {
        switch (soundType)
        {
            case SoundType.SE:
                return _channelIdCounter % 2 == 0 ? ChannelGroup.SE_A : ChannelGroup.SE_B;
            case SoundType.ME:
                return ChannelGroup.Jingle;
            case SoundType.Voice:
                return ChannelGroup.Voice;
            default:
                return ChannelGroup.SE_A;
        }
    }

    private AudioSource GetAvailableAudioSource(ChannelGroup group, int requestedPriority)
    {
        if (!_audioSourcePools.TryGetValue(group, out var pool))
        {
            return null;
        }

        foreach (var source in pool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        AudioSource lowestPrioritySource = null;
        int lowestPriority = int.MaxValue;
        float oldestStartTime = float.MaxValue;

        foreach (var kvp in _activePlaybacks)
        {
            var info = kvp.Value;
            if (info.Group != group) continue;

            if (info.Priority < lowestPriority ||
                (info.Priority == lowestPriority && info.StartTime < oldestStartTime))
            {
                if (requestedPriority >= info.Priority)
                {
                    lowestPriority = info.Priority;
                    oldestStartTime = info.StartTime;
                    lowestPrioritySource = info.AudioSource;
                }
            }
        }

        if (lowestPrioritySource != null)
        {
            string playerIdToRemove = null;
            foreach (var kvp in _activePlaybacks)
            {
                if (kvp.Value.AudioSource == lowestPrioritySource)
                {
                    playerIdToRemove = kvp.Key;
                    break;
                }
            }
            if (playerIdToRemove != null)
            {
                StopPlayback(playerIdToRemove);
            }
        }

        return lowestPrioritySource;
    }

    private void StopPlayback(string playerId)
    {
        if (_activePlaybacks.TryGetValue(playerId, out var info))
        {
            if (info.AudioSource != null)
            {
                info.AudioSource.Stop();
                info.AudioSource.clip = null;
            }
            _activePlaybacks.Remove(playerId);
        }
    }

    private void Update()
    {
        var finishedPlayers = new List<string>();

        foreach (var kvp in _activePlaybacks)
        {
            if (!kvp.Value.IsPlaying && !kvp.Value.AudioSource.loop)
            {
                finishedPlayers.Add(kvp.Key);
            }
        }

        foreach (var playerId in finishedPlayers)
        {
            _activePlaybacks.Remove(playerId);
        }
    }

    public void Dispose()
    {
        _disposables?.Dispose();

        foreach (var info in _activePlaybacks.Values)
        {
            if (info.AudioSource != null)
            {
                info.AudioSource.Stop();
            }
        }
        _activePlaybacks.Clear();
        _soundDataMap.Clear();

        if (_instance == this)
        {
            _instance = null;
        }
    }
}
