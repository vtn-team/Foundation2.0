using UnityEngine;

/// <summary>
/// 使用するInjectParamListを選択するための設定アセット
/// </summary>
[CreateAssetMenu(fileName = "ParamInjectSettings", menuName = "Game/Inject/ParamInjectSettings")]
public class ParamInjectSettings : ScriptableObject
{
    /// <summary>
    /// 選択されたInjectParamList
    /// </summary>
    [SerializeField]
    private InjectParamList _selectedParamList;

    /// <summary>
    /// 自動生成を有効にするか
    /// </summary>
    [SerializeField]
    private bool _autoGenerate = true;

    /// <summary>
    /// 生成コードの出力パス
    /// </summary>
    [SerializeField]
    private string _generatedCodePath = "Assets/Feature/Inject/Generated/";

    /// <summary>
    /// 選択されたInjectParamListを取得
    /// </summary>
    public InjectParamList SelectedParamList
    {
        get => _selectedParamList;
        set => _selectedParamList = value;
    }

    /// <summary>
    /// 自動生成が有効かどうか
    /// </summary>
    public bool AutoGenerate => _autoGenerate;

    /// <summary>
    /// 生成コードの出力パス
    /// </summary>
    public string GeneratedCodePath => _generatedCodePath;
}
