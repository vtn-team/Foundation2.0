# SoundManager

## 概要
インゲームにおけるサウンド再生を統括するシングルトンクラス。
SoundDictionaryを読み込んでリソースを管理し、チャンネル（AudioSource）の割り当てとAudioMixerの出力制御を行う。
SoundPlayerからの再生命令を受け取り、再生状態を追跡・管理する。

## 責務
- SoundDictionaryの読み込みとリソース管理
- AudioMixerグループの出力管理
- チャンネル（AudioSource）のプーリングと割り当て
- SoundPlayerからの再生命令の受付と状態保持
- 再生中のプレイヤー情報の追跡
- 優先度に基づくチャンネル割り当て制御

## チャンネル構成
デフォルト構成（SerializeFieldで拡張可能）:

| カテゴリ | チャンネル数 | 用途 |
|---------|-------------|------|
| SE Group A | 3ch | アセットボリューム管理用 |
| SE Group B | 3ch | アセットボリューム管理用 |
| Jingle | 1ch | ジングル再生用 |
| Voice | 1ch | ボイス再生用 |

## クラス定義

```csharp
namespace BaseSystem.Sound.InGame
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Audio;
    using Cysharp.Threading.Tasks;
    using R3;

    /// <summary>
    /// サウンドタイプ
    /// </summary>
    public enum SoundType
    {
        SE,
        ME,      // ミュージックエフェクト（ジングル）
        Voice
    }

    /// <summary>
    /// チャンネルグループ
    /// </summary>
    public enum ChannelGroup
    {
        SE_A,
        SE_B,
        Jingle,
        Voice
    }

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
        #region Singleton
        private static SoundManager instance;
        public static SoundManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("SoundManager");
                    instance = go.AddComponent<SoundManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
        #endregion

        #region SerializeField
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
        #endregion

        #region Private Fields
        // グループごとのAudioSourceプール
        private Dictionary<ChannelGroup, List<AudioSource>> audioSourcePools;

        // SoundDictionaryのマージ済みデータ
        private Dictionary<string, SoundDictionaryItem> soundDataMap;

        // 再生中のプレイヤー情報
        private Dictionary<string, PlaybackInfo> activePlaybacks;

        // チャンネルIDカウンター
        private int channelIdCounter;

        // 初期化済みフラグ
        private bool isInitialized;

        // Dispose用
        private readonly CompositeDisposable disposables = new CompositeDisposable();
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void OnDestroy()
        {
            Dispose();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// 初期化
        /// </summary>
        private void Initialize()
        {
            if (isInitialized) return;

            audioSourcePools = new Dictionary<ChannelGroup, List<AudioSource>>();
            soundDataMap = new Dictionary<string, SoundDictionaryItem>();
            activePlaybacks = new Dictionary<string, PlaybackInfo>();
            channelIdCounter = 0;

            // AudioSourceプールの初期化
            InitializeAudioSourcePools();

            // SoundDictionaryの読み込み
            LoadSoundDictionaries();

            isInitialized = true;
            Debug.Log("[SoundManager] 初期化完了");
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

                    // AudioMixerGroupの設定
                    if (config.MixerGroup != null)
                    {
                        audioSource.outputAudioMixerGroup = config.MixerGroup;
                    }

                    pool.Add(audioSource);
                }

                audioSourcePools[config.Group] = pool;
            }
        }

        /// <summary>
        /// SoundDictionaryの読み込み
        /// </summary>
        private void LoadSoundDictionaries()
        {
            soundDataMap.Clear();

            if (soundDictionaries == null) return;

            foreach (var dictionary in soundDictionaries)
            {
                if (dictionary == null) continue;

                foreach (var item in dictionary.Items)
                {
                    if (string.IsNullOrEmpty(item.Key)) continue;

                    if (soundDataMap.ContainsKey(item.Key))
                    {
                        Debug.LogWarning($"[SoundManager] 重複キー検出: {item.Key}");
                        continue;
                    }

                    soundDataMap[item.Key] = item;
                }
            }

            Debug.Log($"[SoundManager] {soundDataMap.Count}個のサウンドデータを読み込み");
        }
        #endregion

        #region Play API
        /// <summary>
        /// 再生リクエスト（SoundPlayerから呼び出し）
        /// </summary>
        /// <param name="playerId">SoundPlayerのUUID</param>
        /// <param name="soundKey">再生キー</param>
        /// <param name="volume">音量（0.0〜1.0）</param>
        /// <param name="loop">ループ再生</param>
        /// <returns>チャンネルID（-1は失敗）</returns>
        public int RequestPlay(string playerId, string soundKey, float volume = 1.0f, bool loop = false)
        {
            // サウンドデータの取得
            if (!soundDataMap.TryGetValue(soundKey, out var soundItem))
            {
                Debug.LogWarning($"[SoundManager] サウンドキーが見つかりません: {soundKey}");
                return -1;
            }

            // チャンネルグループの決定
            var group = GetChannelGroupForSoundType(soundItem.SoundType);

            // 利用可能なAudioSourceを取得
            var audioSource = GetAvailableAudioSource(group, soundItem.Priority);
            if (audioSource == null)
            {
                Debug.LogWarning($"[SoundManager] 利用可能なチャンネルがありません: {group}");
                return -1;
            }

            // 再生情報の作成
            var channelId = ++channelIdCounter;
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

            // 既存の再生を停止（同一プレイヤー）
            if (activePlaybacks.ContainsKey(playerId))
            {
                StopPlayback(playerId);
            }

            // 再生情報を登録
            activePlaybacks[playerId] = playbackInfo;

            // 再生開始
            audioSource.clip = soundItem.AudioClip;
            audioSource.volume = volume;
            audioSource.loop = loop;
            audioSource.Play();

            Debug.Log($"[SoundManager] 再生開始: {soundKey} (Player: {playerId}, Channel: {channelId})");

            return channelId;
        }

        /// <summary>
        /// 停止リクエスト
        /// </summary>
        /// <param name="playerId">SoundPlayerのUUID</param>
        public void RequestStop(string playerId)
        {
            StopPlayback(playerId);
        }

        /// <summary>
        /// 一時停止リクエスト
        /// </summary>
        /// <param name="playerId">SoundPlayerのUUID</param>
        public void RequestPause(string playerId)
        {
            if (activePlaybacks.TryGetValue(playerId, out var info))
            {
                info.AudioSource?.Pause();
            }
        }

        /// <summary>
        /// 再開リクエスト
        /// </summary>
        /// <param name="playerId">SoundPlayerのUUID</param>
        public void RequestResume(string playerId)
        {
            if (activePlaybacks.TryGetValue(playerId, out var info))
            {
                info.AudioSource?.UnPause();
            }
        }
        #endregion

        #region Query API
        /// <summary>
        /// 再生状態を確認
        /// </summary>
        /// <param name="playerId">SoundPlayerのUUID</param>
        /// <returns>再生中ならtrue</returns>
        public bool IsPlaying(string playerId)
        {
            if (activePlaybacks.TryGetValue(playerId, out var info))
            {
                return info.IsPlaying;
            }
            return false;
        }

        /// <summary>
        /// 再生情報を取得
        /// </summary>
        /// <param name="playerId">SoundPlayerのUUID</param>
        /// <returns>再生情報（存在しない場合はnull）</returns>
        public PlaybackInfo GetPlaybackInfo(string playerId)
        {
            activePlaybacks.TryGetValue(playerId, out var info);
            return info;
        }

        /// <summary>
        /// チャンネルIDで再生状態を確認
        /// </summary>
        /// <param name="channelId">チャンネルID</param>
        /// <returns>再生中ならtrue</returns>
        public bool IsPlayingByChannel(int channelId)
        {
            foreach (var info in activePlaybacks.Values)
            {
                if (info.ChannelId == channelId)
                {
                    return info.IsPlaying;
                }
            }
            return false;
        }

        /// <summary>
        /// サウンドデータを取得
        /// </summary>
        /// <param name="soundKey">再生キー</param>
        /// <returns>サウンドデータ（存在しない場合はnull）</returns>
        public SoundDictionaryItem GetSoundData(string soundKey)
        {
            soundDataMap.TryGetValue(soundKey, out var item);
            return item;
        }
        #endregion

        #region Volume Control
        /// <summary>
        /// マスター音量を設定
        /// </summary>
        /// <param name="volume">音量（0.0〜1.0）</param>
        public void SetMasterVolume(float volume)
        {
            if (audioMixer != null)
            {
                // dB変換（0〜1を-80dB〜0dBに変換）
                float db = volume > 0 ? Mathf.Log10(volume) * 20f : -80f;
                audioMixer.SetFloat("MasterVolume", db);
            }
        }

        /// <summary>
        /// カテゴリ音量を設定
        /// </summary>
        /// <param name="group">チャンネルグループ</param>
        /// <param name="volume">音量（0.0〜1.0）</param>
        public void SetGroupVolume(ChannelGroup group, float volume)
        {
            if (audioMixer != null)
            {
                string paramName = $"{group}Volume";
                float db = volume > 0 ? Mathf.Log10(volume) * 20f : -80f;
                audioMixer.SetFloat(paramName, db);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// SoundTypeに対応するChannelGroupを取得
        /// </summary>
        private ChannelGroup GetChannelGroupForSoundType(SoundType soundType)
        {
            switch (soundType)
            {
                case SoundType.SE:
                    // SE_AとSE_Bを交互に使用（負荷分散）
                    return channelIdCounter % 2 == 0 ? ChannelGroup.SE_A : ChannelGroup.SE_B;
                case SoundType.ME:
                    return ChannelGroup.Jingle;
                case SoundType.Voice:
                    return ChannelGroup.Voice;
                default:
                    return ChannelGroup.SE_A;
            }
        }

        /// <summary>
        /// 利用可能なAudioSourceを取得
        /// </summary>
        private AudioSource GetAvailableAudioSource(ChannelGroup group, int requestedPriority)
        {
            if (!audioSourcePools.TryGetValue(group, out var pool))
            {
                return null;
            }

            // 未使用のAudioSourceを探す
            foreach (var source in pool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            // 全て使用中の場合、優先度の低いものを探す
            AudioSource lowestPrioritySource = null;
            int lowestPriority = int.MaxValue;
            float oldestStartTime = float.MaxValue;

            foreach (var kvp in activePlaybacks)
            {
                var info = kvp.Value;
                if (info.Group != group) continue;

                // 優先度が低い、または同じ優先度で古いものを探す
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

            // 見つかった場合、その再生を停止
            if (lowestPrioritySource != null)
            {
                // 対応するPlaybackInfoを削除
                string playerIdToRemove = null;
                foreach (var kvp in activePlaybacks)
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

        /// <summary>
        /// 再生を停止
        /// </summary>
        private void StopPlayback(string playerId)
        {
            if (activePlaybacks.TryGetValue(playerId, out var info))
            {
                if (info.AudioSource != null)
                {
                    info.AudioSource.Stop();
                    info.AudioSource.clip = null;
                }
                activePlaybacks.Remove(playerId);
                Debug.Log($"[SoundManager] 再生停止: Player {playerId}");
            }
        }

        /// <summary>
        /// 終了した再生の自動クリーンアップ
        /// </summary>
        private void Update()
        {
            // 再生終了したエントリを検出
            var finishedPlayers = new List<string>();

            foreach (var kvp in activePlaybacks)
            {
                if (!kvp.Value.IsPlaying && !kvp.Value.AudioSource.loop)
                {
                    finishedPlayers.Add(kvp.Key);
                }
            }

            // クリーンアップ
            foreach (var playerId in finishedPlayers)
            {
                activePlaybacks.Remove(playerId);
            }
        }
        #endregion

        #region Dispose
        /// <summary>
        /// リソースの破棄
        /// </summary>
        public void Dispose()
        {
            disposables?.Dispose();

            // 全再生を停止
            foreach (var info in activePlaybacks.Values)
            {
                if (info.AudioSource != null)
                {
                    info.AudioSource.Stop();
                }
            }
            activePlaybacks.Clear();

            soundDataMap.Clear();

            if (instance == this)
            {
                instance = null;
            }
        }
        #endregion
    }
}
```

## 使用例

### 基本的な再生
```csharp
// SoundPlayerを通じて再生（推奨）
var soundPlayer = GetComponent<SoundPlayer>();
soundPlayer.Play("jump_se");

// SoundManagerを直接使用（非推奨）
var channelId = SoundManager.Instance.RequestPlay("player-uuid", "jump_se");
```

### 再生状態の確認
```csharp
// SoundPlayer経由
if (soundPlayer.IsPlaying)
{
    Debug.Log("再生中");
}

// SoundManager経由
var playbackInfo = SoundManager.Instance.GetPlaybackInfo(soundPlayer.PlayerId);
if (playbackInfo != null && playbackInfo.IsPlaying)
{
    Debug.Log($"チャンネル {playbackInfo.ChannelId} で再生中");
}
```

### 音量制御
```csharp
// マスター音量
SoundManager.Instance.SetMasterVolume(0.8f);

// グループ音量
SoundManager.Instance.SetGroupVolume(ChannelGroup.SE_A, 0.5f);
SoundManager.Instance.SetGroupVolume(ChannelGroup.Voice, 1.0f);
```

## 設計方針
- **プレイヤーベース管理**: SoundPlayerのUUIDで再生状態を追跡
- **チャンネルプーリング**: グループごとにAudioSourceをプール
- **優先度制御**: 優先度に基づいてチャンネル割り当てを決定
- **AudioMixer統合**: グループごとの音量制御をサポート
- **自動クリーンアップ**: 再生終了した情報を自動的に削除

## 配置場所
- スクリプト: `unity/Assets/Scripts/System/Sound/SoundManager.cs`

## 依存関係
- UnityEngine
- UnityEngine.Audio
- UniTask
- R3
- SoundDictionary.cs
- SoundPlayer.cs

## 注意事項
- **SoundPlayerを使用**: 直接SoundManagerを呼び出すよりSoundPlayer経由を推奨
- **SerializeField設定**: InspectorでSoundDictionaryとAudioMixerを設定すること
- **チャンネル拡張**: channelConfigsで任意のチャンネル構成が可能
