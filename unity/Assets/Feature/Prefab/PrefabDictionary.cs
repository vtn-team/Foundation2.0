using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prefab辞書のアイテム
/// </summary>
[Serializable]
public class PrefabDicItem
{
    /// <summary>
    /// アクセスに使用する名前
    /// </summary>
    public string keyName;

    /// <summary>
    /// Prefabの参照
    /// </summary>
    public GameObject prefab;

    /// <summary>
    /// 生成上限値
    /// </summary>
    public int limit = 10;
}

/// <summary>
/// ゲーム中に使用するPrefabの情報をまとめるScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "PrefabDictionary", menuName = "Game/PrefabDictionary")]
public class PrefabDictionary : ScriptableObject
{
    [SerializeField]
    private List<PrefabDicItem> prefabDicList = new List<PrefabDicItem>();

    private Dictionary<string, PrefabDicItem> _dictionary;

    /// <summary>
    /// Prefabリストを取得
    /// </summary>
    public List<PrefabDicItem> PrefabDicList => prefabDicList;

    /// <summary>
    /// 辞書を初期化・取得
    /// </summary>
    private Dictionary<string, PrefabDicItem> Dictionary
    {
        get
        {
            if (_dictionary == null)
            {
                BuildDictionary();
            }
            return _dictionary;
        }
    }

    /// <summary>
    /// リストから辞書を構築
    /// </summary>
    private void BuildDictionary()
    {
        _dictionary = new Dictionary<string, PrefabDicItem>();
        foreach (var item in prefabDicList)
        {
            if (!string.IsNullOrEmpty(item.keyName) && !_dictionary.ContainsKey(item.keyName))
            {
                _dictionary[item.keyName] = item;
            }
        }
    }

    /// <summary>
    /// 辞書を再構築
    /// </summary>
    public void RebuildDictionary()
    {
        BuildDictionary();
    }

    /// <summary>
    /// キーに対応するPrefabを返す
    /// </summary>
    /// <param name="key">キー</param>
    /// <returns>Prefab（見つからない場合はnull）</returns>
    public GameObject GetPrefab(string key)
    {
        if (Dictionary.TryGetValue(key, out var item))
        {
            return item.prefab;
        }
        Debug.LogWarning($"[PrefabDictionary] Key not found: {key}");
        return null;
    }

    /// <summary>
    /// キーに対応するPrefabDicItemを返す
    /// </summary>
    /// <param name="key">キー</param>
    /// <returns>PrefabDicItem（見つからない場合はnull）</returns>
    public PrefabDicItem GetItem(string key)
    {
        if (Dictionary.TryGetValue(key, out var item))
        {
            return item;
        }
        return null;
    }

    /// <summary>
    /// キーをstringの配列で返す
    /// </summary>
    /// <returns>キーの配列</returns>
    public string[] GetKeyList()
    {
        var keys = new List<string>();
        foreach (var item in prefabDicList)
        {
            if (!string.IsNullOrEmpty(item.keyName))
            {
                keys.Add(item.keyName);
            }
        }
        return keys.ToArray();
    }

    /// <summary>
    /// 指定したキーが登録されているかを確認する
    /// </summary>
    /// <param name="key">キー</param>
    /// <returns>登録されている場合はtrue</returns>
    public bool IsKeyRegistered(string key)
    {
        return Dictionary.ContainsKey(key);
    }

    /// <summary>
    /// 新しいPrefabを登録する
    /// </summary>
    /// <param name="key">キー</param>
    /// <param name="prefab">Prefab参照</param>
    /// <param name="limit">生成上限値（デフォルト10）</param>
    public void RegisterPrefab(string key, GameObject prefab, int limit = 10)
    {
        if (IsKeyRegistered(key))
        {
            Debug.LogWarning($"[PrefabDictionary] Key already exists: {key}");
            return;
        }

        var item = new PrefabDicItem
        {
            keyName = key,
            prefab = prefab,
            limit = limit
        };

        prefabDicList.Add(item);
        Dictionary[key] = item;
    }

    /// <summary>
    /// 既存のPrefabを更新する
    /// </summary>
    /// <param name="key">キー</param>
    /// <param name="prefab">新しいPrefab参照</param>
    /// <param name="limit">生成上限値（デフォルト10）</param>
    public void UpdatePrefab(string key, GameObject prefab, int limit = 10)
    {
        if (!IsKeyRegistered(key))
        {
            Debug.LogWarning($"[PrefabDictionary] Key not found: {key}");
            return;
        }

        var item = Dictionary[key];
        item.prefab = prefab;
        item.limit = limit;
    }

    /// <summary>
    /// PrefabDicItemをリストに追加する
    /// </summary>
    /// <param name="item">追加するアイテム</param>
    public void AddList(PrefabDicItem item)
    {
        prefabDicList.Add(item);
        if (!string.IsNullOrEmpty(item.keyName))
        {
            Dictionary[item.keyName] = item;
        }
    }
}
