using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// パラメータ情報
/// </summary>
[Serializable]
public class ParamInfo
{
    /// <summary>
    /// グループ名
    /// </summary>
    public string Group;

    /// <summary>
    /// 変数名
    /// </summary>
    public string VarName;

    /// <summary>
    /// 表示名（日本語）
    /// </summary>
    public string ViewName;
}

/// <summary>
/// 注入先の変数名の説明を記載するアセット
/// </summary>
[CreateAssetMenu(fileName = "InjectParamInfo", menuName = "Game/Inject/InjectParamInfo")]
public class InjectParamInfo : ScriptableObject
{
    /// <summary>
    /// パラメータ情報リスト
    /// </summary>
    [SerializeField]
    private List<ParamInfo> _paramInfoList = new List<ParamInfo>();

    /// <summary>
    /// パラメータ情報リストを取得
    /// </summary>
    public List<ParamInfo> ParamInfoList => _paramInfoList;

    /// <summary>
    /// 変数名から表示名を取得
    /// </summary>
    /// <param name="varName">変数名</param>
    /// <returns>表示名（見つからない場合は変数名をそのまま返す）</returns>
    public string GetViewName(string varName)
    {
        var info = _paramInfoList.Find(p => p.VarName == varName);
        return info != null ? info.ViewName : varName;
    }

    /// <summary>
    /// グループ名からパラメータ情報を取得
    /// </summary>
    /// <param name="group">グループ名</param>
    /// <returns>パラメータ情報リスト</returns>
    public List<ParamInfo> GetByGroup(string group)
    {
        return _paramInfoList.FindAll(p => p.Group == group);
    }

    /// <summary>
    /// すべてのグループ名を取得
    /// </summary>
    /// <returns>グループ名のリスト</returns>
    public List<string> GetAllGroups()
    {
        var groups = new HashSet<string>();
        foreach (var info in _paramInfoList)
        {
            if (!string.IsNullOrEmpty(info.Group))
            {
                groups.Add(info.Group);
            }
        }
        return new List<string>(groups);
    }
}
