using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AudioClipバリエーション
/// </summary>
[Serializable]
public class SoundClipVariant
{
    [SerializeField]
    [Tooltip("音声ファイル")]
    private AudioClip clip;

    [SerializeField]
    [Tooltip("バリエーションのラベル（任意）")]
    private string label;

    public AudioClip Clip => clip;
    public string Label => label;
    public bool IsValid => clip != null;
    public float Length => clip != null ? clip.length : 0f;
}

/// <summary>
/// バリエーション対応サウンドアイテム
/// </summary>
[Serializable]
public class SoundVariantDictionaryItem
{
    [Header("Identification")]
    [SerializeField]
    [Tooltip("再生キー（一意の識別子）")]
    private string key;

    [Header("Variants")]
    [SerializeField]
    [Tooltip("AudioClipバリエーションリスト")]
    private List<SoundClipVariant> variants = new List<SoundClipVariant>();

    [Header("Control")]
    [SerializeField]
    [Tooltip("グループID")]
    private int groupId;

    [SerializeField]
    [Range(0, 100)]
    [Tooltip("優先度")]
    private int priority = 50;

    public string Key => key;
    public IReadOnlyList<SoundClipVariant> Variants => variants;
    public int GroupId => groupId;
    public int Priority => priority;
    public SoundType SoundType { get; internal set; }
    public string Category { get; internal set; }
    public int VariantCount => variants.Count;

    public int ValidVariantCount
    {
        get
        {
            int count = 0;
            foreach (var v in variants)
            {
                if (v.IsValid) count++;
            }
            return count;
        }
    }

    public bool IsValid => !string.IsNullOrEmpty(key) && variants.Count > 0 && HasValidVariant();

    private bool HasValidVariant()
    {
        foreach (var v in variants)
        {
            if (v.IsValid) return true;
        }
        return false;
    }

    public AudioClip GetClipByIndex(int index)
    {
        if (index < 0 || index >= variants.Count) return null;
        return variants[index].Clip;
    }

    public AudioClip GetFirstClip()
    {
        foreach (var v in variants)
        {
            if (v.IsValid) return v.Clip;
        }
        return null;
    }

    public SoundClipVariant GetVariant(int index)
    {
        if (index < 0 || index >= variants.Count) return null;
        return variants[index];
    }

    public AudioClip GetClipByLabel(string label)
    {
        foreach (var v in variants)
        {
            if (v.Label == label && v.IsValid) return v.Clip;
        }
        return null;
    }
}

/// <summary>
/// バリエーション対応サウンドディクショナリ
/// </summary>
[CreateAssetMenu(fileName = "SoundVariantDictionary", menuName = "Sound/SoundVariantDictionary")]
public class SoundVariantDictionary : SoundDictionary
{
    [Header("Variant Items")]
    [SerializeField]
    [Tooltip("バリエーションアイテムリスト")]
    private List<SoundVariantDictionaryItem> variantItems = new List<SoundVariantDictionaryItem>();

    private Dictionary<string, SoundVariantDictionaryItem> _variantItemMap;

    public IReadOnlyList<SoundVariantDictionaryItem> VariantItems => variantItems;
    public int VariantItemCount => variantItems.Count;

    public override void Initialize()
    {
        if (isInitialized) return;

        base.Initialize();

        _variantItemMap = new Dictionary<string, SoundVariantDictionaryItem>();

        foreach (var item in variantItems)
        {
            if (!item.IsValid) continue;

            item.SoundType = soundType;
            item.Category = category;

            if (!_variantItemMap.ContainsKey(item.Key))
            {
                _variantItemMap[item.Key] = item;
            }
        }
    }

    public SoundVariantDictionaryItem GetVariantItem(string key)
    {
        if (!isInitialized) Initialize();
        _variantItemMap.TryGetValue(key, out var item);
        return item;
    }

    public bool TryGetVariantItem(string key, out SoundVariantDictionaryItem item)
    {
        if (!isInitialized) Initialize();
        return _variantItemMap.TryGetValue(key, out item);
    }

    public AudioClip GetClipByIndex(string key, int index)
    {
        if (TryGetVariantItem(key, out var item))
        {
            return item.GetClipByIndex(index);
        }
        return null;
    }

    public AudioClip GetFirstClip(string key)
    {
        if (TryGetVariantItem(key, out var item))
        {
            return item.GetFirstClip();
        }
        return null;
    }

    public int GetVariantCount(string key)
    {
        if (TryGetVariantItem(key, out var item))
        {
            return item.VariantCount;
        }
        return 0;
    }

    public bool ContainsVariantKey(string key)
    {
        if (!isInitialized) Initialize();
        return _variantItemMap.ContainsKey(key);
    }

    public IEnumerable<string> GetAllVariantKeys()
    {
        if (!isInitialized) Initialize();
        return _variantItemMap.Keys;
    }
}
