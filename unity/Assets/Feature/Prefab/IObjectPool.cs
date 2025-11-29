using UnityEngine;

/// <summary>
/// PrefabStockで管理されるオブジェクトが実装するインターフェース
/// </summary>
public interface IObjectPool
{
    /// <summary>
    /// オブジェクト生成時に1回呼ばれる
    /// </summary>
    /// <param name="prefabKey">プレファブキー</param>
    void OnPoolInstantiate(string prefabKey);

    /// <summary>
    /// プールから取り出される度に呼ばれる
    /// </summary>
    void OnPoolUse();

    /// <summary>
    /// プールに返却される度に呼ばれる
    /// </summary>
    void OnPoolRelease();

    /// <summary>
    /// オブジェクト破棄時に呼ばれる
    /// </summary>
    void OnPoolDestroy();
}
