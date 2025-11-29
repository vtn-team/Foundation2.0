using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ボイス用AudioClipバリエーション
/// </summary>
[Serializable]
public class VoiceClipVariation
{
    [SerializeField]
    [Tooltip("音声ファイル")]
    private AudioClip clip;

    [SerializeField]
    [Tooltip("出現率（重み）")]
    private int weight = 1;

    public AudioClip Clip => clip;
    public int Weight => weight;
    public bool IsValid => clip != null && weight > 0;
}

/// <summary>
/// ボイス用サウンドアイテム
/// </summary>
[Serializable]
public class VoiceSoundDictionaryItem
{
    [Header("Identification")]
    [SerializeField]
    [Tooltip("再生キー（一意の識別子）")]
    private string key;

    [Header("Variations")]
    [SerializeField]
    [Tooltip("AudioClipバリエーションリスト")]
    private List<VoiceClipVariation> variations = new List<VoiceClipVariation>();

    [Header("Control")]
    [SerializeField]
    [Tooltip("グループID")]
    private int groupId;

    [SerializeField]
    [Range(0, 100)]
    [Tooltip("優先度")]
    private int priority = 50;

    public string Key => key;
    public IReadOnlyList<VoiceClipVariation> Variations => variations;
    public int GroupId => groupId;
    public int Priority => priority;
    public int VariationCount => variations.Count;
    public bool IsValid => !string.IsNullOrEmpty(key) && variations.Count > 0;

    public int TotalWeight
    {
        get
        {
            int total = 0;
            foreach (var v in variations)
            {
                if (v.IsValid) total += v.Weight;
            }
            return total;
        }
    }

    /// <summary>
    /// 重みづけ確率でランダムにAudioClipを取得
    /// </summary>
    public AudioClip GetRandomClip()
    {
        if (variations.Count == 0) return null;

        if (variations.Count == 1)
        {
            return variations[0].IsValid ? variations[0].Clip : null;
        }

        int totalWeight = TotalWeight;

        if (totalWeight <= 0)
        {
            return GetFirstValidClip();
        }

        int random = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;

        foreach (var variation in variations)
        {
            if (!variation.IsValid) continue;

            cumulative += variation.Weight;
            if (random < cumulative)
            {
                return variation.Clip;
            }
        }

        return GetLastValidClip();
    }

    private AudioClip GetFirstValidClip()
    {
        foreach (var variation in variations)
        {
            if (variation.IsValid) return variation.Clip;
        }
        return null;
    }

    private AudioClip GetLastValidClip()
    {
        for (int i = variations.Count - 1; i >= 0; i--)
        {
            if (variations[i].IsValid) return variations[i].Clip;
        }
        return null;
    }

    public AudioClip GetClipByIndex(int index)
    {
        if (index >= 0 && index < variations.Count)
        {
            return variations[index].Clip;
        }
        return null;
    }
}

/// <summary>
/// ボイス用サウンドディクショナリ
/// </summary>
[CreateAssetMenu(fileName = "VoiceSoundDictionary", menuName = "Sound/VoiceSoundDictionary")]
public class VoiceSoundDictionary : SoundDictionary
{
    [Header("Voice Items")]
    [SerializeField]
    [Tooltip("ボイスアイテムリスト")]
    private List<VoiceSoundDictionaryItem> voiceItems = new List<VoiceSoundDictionaryItem>();

    private Dictionary<string, VoiceSoundDictionaryItem> _voiceItemMap;

    public IReadOnlyList<VoiceSoundDictionaryItem> VoiceItems => voiceItems;

    public override void Initialize()
    {
        if (isInitialized) return;

        base.Initialize();

        _voiceItemMap = new Dictionary<string, VoiceSoundDictionaryItem>();

        foreach (var item in voiceItems)
        {
            if (!item.IsValid) continue;

            if (!_voiceItemMap.ContainsKey(item.Key))
            {
                _voiceItemMap[item.Key] = item;
            }
        }
    }

    public VoiceSoundDictionaryItem GetVoiceItem(string key)
    {
        if (!isInitialized) Initialize();
        _voiceItemMap.TryGetValue(key, out var item);
        return item;
    }

    public bool TryGetVoiceItem(string key, out VoiceSoundDictionaryItem item)
    {
        if (!isInitialized) Initialize();
        return _voiceItemMap.TryGetValue(key, out item);
    }

    public AudioClip GetRandomClip(string key)
    {
        if (TryGetVoiceItem(key, out var item))
        {
            return item.GetRandomClip();
        }
        return null;
    }

    public bool ContainsVoiceKey(string key)
    {
        if (!isInitialized) Initialize();
        return _voiceItemMap.ContainsKey(key);
    }
}
