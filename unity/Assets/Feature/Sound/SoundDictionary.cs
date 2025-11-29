using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// サウンドディクショナリアイテム
/// </summary>
[Serializable]
public class SoundDictionaryItem
{
    [Header("Identification")]
    [SerializeField]
    [Tooltip("再生キー（一意の識別子）")]
    private string key;

    [Header("Audio")]
    [SerializeField]
    [Tooltip("AudioClip参照")]
    private AudioClip audioClip;

    [Header("Control")]
    [SerializeField]
    [Tooltip("グループID（同一グループは排他制御など）")]
    private int groupId;

    [SerializeField]
    [Range(0, 100)]
    [Tooltip("優先度（高いほど再生優先）")]
    private int priority = 50;

    /// <summary>
    /// 再生キー
    /// </summary>
    public string Key => key;

    /// <summary>
    /// AudioClip
    /// </summary>
    public AudioClip AudioClip => audioClip;

    /// <summary>
    /// グループID
    /// </summary>
    public int GroupId => groupId;

    /// <summary>
    /// 優先度
    /// </summary>
    public int Priority => priority;

    /// <summary>
    /// サウンドタイプ（親のSoundDictionaryから参照）
    /// </summary>
    public SoundType SoundType { get; internal set; }

    /// <summary>
    /// カテゴリ（親のSoundDictionaryから参照）
    /// </summary>
    public string Category { get; internal set; }

    /// <summary>
    /// 有効なアイテムかどうか
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(key) && audioClip != null;

    /// <summary>
    /// AudioClipの長さ（秒）
    /// </summary>
    public float Length => audioClip != null ? audioClip.length : 0f;
}

/// <summary>
/// サウンドディクショナリ（ScriptableObject）
/// </summary>
[CreateAssetMenu(fileName = "SoundDictionary", menuName = "Sound/SoundDictionary")]
public class SoundDictionary : ScriptableObject
{
    [Header("Dictionary Settings")]
    [SerializeField]
    [Tooltip("カテゴリ名（例: Player, Enemy, UI）")]
    protected string category = "Default";

    [SerializeField]
    [Tooltip("再生タイプ")]
    protected SoundType soundType = SoundType.SE;

    [Header("Sound Items")]
    [SerializeField]
    [Tooltip("サウンドアイテムリスト")]
    protected List<SoundDictionaryItem> items = new List<SoundDictionaryItem>();

    /// <summary>
    /// カテゴリ名
    /// </summary>
    public string Category => category;

    /// <summary>
    /// 再生タイプ
    /// </summary>
    public SoundType SoundType => soundType;

    /// <summary>
    /// サウンドアイテムリスト
    /// </summary>
    public IReadOnlyList<SoundDictionaryItem> Items => items;

    /// <summary>
    /// アイテム数
    /// </summary>
    public int Count => items.Count;

    // キーからアイテムへのマッピング（キャッシュ）
    protected Dictionary<string, SoundDictionaryItem> _itemMap;

    // グループIDからアイテムリストへのマッピング（キャッシュ）
    protected Dictionary<int, List<SoundDictionaryItem>> _groupMap;

    // 初期化済みフラグ
    protected bool isInitialized;

    private void OnEnable()
    {
        Initialize();
    }

    private void OnValidate()
    {
        isInitialized = false;
        Initialize();
        ValidateKeys();
    }

    /// <summary>
    /// 初期化
    /// </summary>
    public virtual void Initialize()
    {
        if (isInitialized) return;

        _itemMap = new Dictionary<string, SoundDictionaryItem>();
        _groupMap = new Dictionary<int, List<SoundDictionaryItem>>();

        foreach (var item in items)
        {
            if (!item.IsValid) continue;

            // 親情報を設定
            item.SoundType = soundType;
            item.Category = category;

            // キーマップに追加
            if (!_itemMap.ContainsKey(item.Key))
            {
                _itemMap[item.Key] = item;
            }

            // グループマップに追加
            if (!_groupMap.ContainsKey(item.GroupId))
            {
                _groupMap[item.GroupId] = new List<SoundDictionaryItem>();
            }
            _groupMap[item.GroupId].Add(item);
        }

        isInitialized = true;
    }

    /// <summary>
    /// キーの重複チェック
    /// </summary>
    private void ValidateKeys()
    {
        var keySet = new HashSet<string>();
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.Key)) continue;

            if (keySet.Contains(item.Key))
            {
                Debug.LogWarning($"[SoundDictionary] 重複キー検出: {item.Key} in {name}");
            }
            else
            {
                keySet.Add(item.Key);
            }
        }
    }

    /// <summary>
    /// キーからアイテムを取得
    /// </summary>
    public SoundDictionaryItem GetItem(string key)
    {
        if (!isInitialized) Initialize();

        _itemMap.TryGetValue(key, out var item);
        return item;
    }

    /// <summary>
    /// キーからアイテムを取得（Try形式）
    /// </summary>
    public bool TryGetItem(string key, out SoundDictionaryItem item)
    {
        if (!isInitialized) Initialize();

        return _itemMap.TryGetValue(key, out item);
    }

    /// <summary>
    /// キーが存在するかチェック
    /// </summary>
    public bool ContainsKey(string key)
    {
        if (!isInitialized) Initialize();

        return _itemMap.ContainsKey(key);
    }

    /// <summary>
    /// グループIDからアイテムリストを取得
    /// </summary>
    public IReadOnlyList<SoundDictionaryItem> GetItemsByGroup(int groupId)
    {
        if (!isInitialized) Initialize();

        if (_groupMap.TryGetValue(groupId, out var list))
        {
            return list;
        }
        return Array.Empty<SoundDictionaryItem>();
    }

    /// <summary>
    /// 全キーを取得
    /// </summary>
    public IEnumerable<string> GetAllKeys()
    {
        if (!isInitialized) Initialize();

        return _itemMap.Keys;
    }

    /// <summary>
    /// 全グループIDを取得
    /// </summary>
    public IEnumerable<int> GetAllGroupIds()
    {
        if (!isInitialized) Initialize();

        return _groupMap.Keys;
    }
}
