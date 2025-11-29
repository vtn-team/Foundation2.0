using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// オブジェクトプールのデータ
/// </summary>
public class ObjectPoolData
{
    public string Key;
    public GameObject Prefab;
    public int Limit;
    public List<GameObject> Pool = new List<GameObject>();
    public List<bool> InUse = new List<bool>();
}

/// <summary>
/// ゲーム中すべてのPrefabはこのクラスからアクセスする
/// Pure Classシングルトン
/// </summary>
public static class PrefabStock
{
    private static PrefabDictionary _prefabDic;
    private static Dictionary<string, ObjectPoolData> _objectPools = new Dictionary<string, ObjectPoolData>();
    private static GameObject _poolRootObject;
    private static bool _isInitialized;

    private const string PREFAB_DICTIONARY_PATH = "Assets/DataAsset/PrefabDictionary.asset";

    /// <summary>
    /// 初期化済みかどうか
    /// </summary>
    public static bool IsInitialized => _isInitialized;

    /// <summary>
    /// PrefabDictionaryを取得
    /// </summary>
    public static PrefabDictionary PrefabDictionary => _prefabDic;

    /// <summary>
    /// PrefabDictionaryを読み込む
    /// </summary>
    private static void LoadPrefabDictionary()
    {
        if (_prefabDic != null) return;

#if UNITY_EDITOR
        _prefabDic = UnityEditor.AssetDatabase.LoadAssetAtPath<PrefabDictionary>(PREFAB_DICTIONARY_PATH);
#else
        _prefabDic = Resources.Load<PrefabDictionary>("PrefabDictionary");
#endif

        if (_prefabDic == null)
        {
            Debug.LogError($"[PrefabStock] PrefabDictionary not found at: {PREFAB_DICTIONARY_PATH}");
        }
    }

    /// <summary>
    /// ゲーム開始時に呼び出し、全プレファブを事前生成する
    /// </summary>
    public static void InitialLoad()
    {
        if (_isInitialized) return;

        LoadPrefabDictionary();
        if (_prefabDic == null) return;

        // プールルートオブジェクトを作成
        _poolRootObject = new GameObject("[PrefabStock Pool]");
        Object.DontDestroyOnLoad(_poolRootObject);

#if UNITY_EDITOR
        _poolRootObject.hideFlags = HideFlags.DontSave;
#endif

        // 全キーに対してプールを準備
        var keys = _prefabDic.GetKeyList();
        foreach (var key in keys)
        {
            var item = _prefabDic.GetItem(key);
            if (item == null || item.prefab == null) continue;

            var poolData = new ObjectPoolData
            {
                Key = key,
                Prefab = item.prefab,
                Limit = item.limit
            };

            // limit数までオブジェクトを事前生成
            for (int i = 0; i < item.limit; i++)
            {
                var instance = CreatePooledInstance(poolData, key);
                instance.SetActive(false);
            }

            _objectPools[key] = poolData;
        }

        _isInitialized = true;
    }

    /// <summary>
    /// プール用インスタンスを生成
    /// </summary>
    private static GameObject CreatePooledInstance(ObjectPoolData poolData, string key)
    {
        var instance = Object.Instantiate(poolData.Prefab, _poolRootObject.transform);

#if UNITY_EDITOR
        instance.hideFlags = HideFlags.DontSave;
#endif

        poolData.Pool.Add(instance);
        poolData.InUse.Add(false);

        // IObjectPoolインターフェースを呼び出す
        var poolable = instance.GetComponent<IObjectPool>();
        poolable?.OnPoolInstantiate(key);

        return instance;
    }

    /// <summary>
    /// Prefabの参照を取得（プール不使用）
    /// </summary>
    /// <param name="key">キー</param>
    /// <returns>Prefab</returns>
    public static GameObject PrefabReference(string key)
    {
        LoadPrefabDictionary();
        return _prefabDic?.GetPrefab(key);
    }

    /// <summary>
    /// プールから空いているインスタンスを返す
    /// </summary>
    /// <param name="key">キー</param>
    /// <returns>インスタンス</returns>
    public static GameObject CreateInstance(string key)
    {
        LoadPrefabDictionary();
        if (_prefabDic == null) return null;

        // プールが未初期化の場合は初期化
        if (!_objectPools.TryGetValue(key, out var poolData))
        {
            var item = _prefabDic.GetItem(key);
            if (item == null || item.prefab == null)
            {
                Debug.LogWarning($"[PrefabStock] Key not found: {key}");
                return null;
            }

            poolData = new ObjectPoolData
            {
                Key = key,
                Prefab = item.prefab,
                Limit = item.limit
            };
            _objectPools[key] = poolData;

            // プールルートがない場合は作成
            if (_poolRootObject == null)
            {
                _poolRootObject = new GameObject("[PrefabStock Pool]");
                Object.DontDestroyOnLoad(_poolRootObject);
#if UNITY_EDITOR
                _poolRootObject.hideFlags = HideFlags.DontSave;
#endif
            }
        }

        // 未使用のインスタンスを探す
        for (int i = 0; i < poolData.Pool.Count; i++)
        {
            if (!poolData.InUse[i] && poolData.Pool[i] != null)
            {
                poolData.InUse[i] = true;
                var instance = poolData.Pool[i];
                instance.SetActive(true);

                // IObjectPoolインターフェースを呼び出す
                var poolable = instance.GetComponent<IObjectPool>();
                poolable?.OnPoolUse();

                return instance;
            }
        }

        // 未使用がなくlimit未満なら新規作成
        if (poolData.Pool.Count < poolData.Limit)
        {
            var instance = CreatePooledInstance(poolData, key);
            poolData.InUse[poolData.Pool.Count - 1] = true;
            instance.SetActive(true);

            var poolable = instance.GetComponent<IObjectPool>();
            poolable?.OnPoolUse();

            return instance;
        }

        // limitを超えた場合は警告を出して新規作成
        Debug.LogWarning($"[PrefabStock] {key}の生成上限({poolData.Limit})を超えました");
        var overInstance = CreatePooledInstance(poolData, key);
        poolData.InUse[poolData.Pool.Count - 1] = true;
        overInstance.SetActive(true);

        var overPoolable = overInstance.GetComponent<IObjectPool>();
        overPoolable?.OnPoolUse();

        return overInstance;
    }

    /// <summary>
    /// インスタンスをプールに返却
    /// </summary>
    /// <param name="instance">返却するインスタンス</param>
    public static void ReleaseInstance(GameObject instance)
    {
        if (instance == null) return;

        foreach (var poolData in _objectPools.Values)
        {
            var index = poolData.Pool.IndexOf(instance);
            if (index >= 0)
            {
                poolData.InUse[index] = false;
                instance.SetActive(false);

                // IObjectPoolインターフェースを呼び出す
                var poolable = instance.GetComponent<IObjectPool>();
                poolable?.OnPoolRelease();

                // プールルートの子に戻す
                if (_poolRootObject != null)
                {
                    instance.transform.SetParent(_poolRootObject.transform);
                }

                return;
            }
        }

        // プールに存在しない場合は破棄
        Object.Destroy(instance);
    }

    /// <summary>
    /// エディタ拡張時に使用。普通にInstantiateする。
    /// </summary>
    /// <param name="key">キー</param>
    /// <returns>インスタンス</returns>
    public static GameObject CreateInstanceEditor(string key)
    {
        LoadPrefabDictionary();
        var prefab = _prefabDic?.GetPrefab(key);
        if (prefab == null) return null;

        return Object.Instantiate(prefab);
    }

    /// <summary>
    /// 全プールをクリア
    /// </summary>
    public static void ClearAllPools()
    {
        foreach (var poolData in _objectPools.Values)
        {
            foreach (var instance in poolData.Pool)
            {
                if (instance != null)
                {
                    var poolable = instance.GetComponent<IObjectPool>();
                    poolable?.OnPoolDestroy();
                    Object.Destroy(instance);
                }
            }
        }

        _objectPools.Clear();

        if (_poolRootObject != null)
        {
            Object.Destroy(_poolRootObject);
            _poolRootObject = null;
        }

        _isInitialized = false;
    }
}
