using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ボックスガチャ設定用のシート
/// </summary>
[CreateAssetMenu(fileName = "BoxSelectionSheet", menuName = "Game/BoxSelectionSheet")]
public class BoxSelectionSheet : ScriptableObject
{
    [Tooltip("ボックス内のオブジェクト設定リスト")]
    public List<BoxObjectData> objectDataList = new List<BoxObjectData>();
}

/// <summary>
/// ボックス内オブジェクトの設定データ
/// </summary>
[Serializable]
public class BoxObjectData
{
    [Tooltip("重みづけ確率")]
    public float prop = 1f;

    [Tooltip("対象物のPrefabKey")]
    public string prefabKey;

    [Tooltip("何個ボックスに入れておくか")]
    public int num = 1;
}

/// <summary>
/// ボックス内オブジェクトの実行時データ
/// </summary>
public class BoxSelectObjectData
{
    /// <summary>
    /// 管理ID（連番）
    /// </summary>
    public int Id;

    /// <summary>
    /// 重みづけ確率
    /// </summary>
    public float Prop;

    /// <summary>
    /// 対象物のPrefabKey
    /// </summary>
    public string PrefabKey;

    /// <summary>
    /// BOX内にいる場合はtrue、排出された場合はfalse
    /// </summary>
    public bool InStock;
}
