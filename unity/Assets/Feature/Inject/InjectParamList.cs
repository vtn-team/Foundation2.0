using UnityEngine;

/// <summary>
/// 注入パラメータリストの基底クラス
/// partial classで自動生成されるプロパティと結合される
/// </summary>
public partial class InjectParamList : ScriptableObject
{
    /// <summary>
    /// リスト名
    /// </summary>
    [SerializeField]
    private string _listName = "";

    /// <summary>
    /// 説明
    /// </summary>
    [SerializeField]
    [TextArea]
    private string _description = "";

    /// <summary>
    /// リスト名を取得
    /// </summary>
    public string ListName => _listName;

    /// <summary>
    /// 説明を取得
    /// </summary>
    public string Description => _description;
}
