using UnityEngine;

/// <summary>
/// 注入用パラメータを保持するシングルトン
/// </summary>
public static class InjectSystem
{
    private static ParamInjectSettings _paramInjectSettings;
    private static bool _isInitialized;

    private const string PARAM_INJECT_SETTINGS_PATH = "Assets/DataAsset/Params/ParamInjectSettings.asset";
    private const string RESOURCES_PATH = "ParamInjectSettings";

    /// <summary>
    /// ParamInjectSettingsを取得
    /// </summary>
    public static ParamInjectSettings ParamInjectSettingsProperty
    {
        get
        {
            if (!_isInitialized)
            {
                Initialize();
            }
            return _paramInjectSettings;
        }
    }

    /// <summary>
    /// ParamInjectSettingsが利用可能かを確認
    /// </summary>
    public static bool IsParamInjectSettingsAvailable()
    {
        if (!_isInitialized)
        {
            Initialize();
        }
        return _paramInjectSettings != null;
    }

    /// <summary>
    /// 初期化処理
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_isInitialized) return;

        LoadParamInjectSettings();
        _isInitialized = true;
    }

    /// <summary>
    /// ParamInjectSettingsを読み込む
    /// </summary>
    private static void LoadParamInjectSettings()
    {
#if UNITY_EDITOR
        // 1. 直接パス読み込みを優先
        _paramInjectSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<ParamInjectSettings>(PARAM_INJECT_SETTINGS_PATH);

        if (_paramInjectSettings == null)
        {
            // 2. フォールバック: Resourcesフォルダから読み込み
            _paramInjectSettings = Resources.Load<ParamInjectSettings>(RESOURCES_PATH);
        }

        if (_paramInjectSettings == null)
        {
            // 3. 自動生成
            AutoGenerateSettings();
        }
#else
        // ビルド時はResourcesから読み込み
        _paramInjectSettings = Resources.Load<ParamInjectSettings>(RESOURCES_PATH);
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// ParamInjectSettingsを自動生成
    /// </summary>
    private static void AutoGenerateSettings()
    {
        // ディレクトリを作成
        var directory = System.IO.Path.GetDirectoryName(PARAM_INJECT_SETTINGS_PATH);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        // アセットを作成
        _paramInjectSettings = ScriptableObject.CreateInstance<ParamInjectSettings>();
        UnityEditor.AssetDatabase.CreateAsset(_paramInjectSettings, PARAM_INJECT_SETTINGS_PATH);
        UnityEditor.AssetDatabase.SaveAssets();

        Debug.Log($"[InjectSystem] ParamInjectSettingsを自動生成しました: {PARAM_INJECT_SETTINGS_PATH}");
    }

    /// <summary>
    /// 手動でParamInjectSettingsを生成
    /// </summary>
    [UnityEditor.MenuItem("Tools/Inject/Create ParamInjectSettings")]
    public static void CreateParamInjectSettings()
    {
        if (UnityEditor.AssetDatabase.LoadAssetAtPath<ParamInjectSettings>(PARAM_INJECT_SETTINGS_PATH) != null)
        {
            Debug.LogWarning("[InjectSystem] ParamInjectSettingsは既に存在します");
            return;
        }

        AutoGenerateSettings();
    }
#endif

    /// <summary>
    /// 初期化状態をリセット（テスト用）
    /// </summary>
    public static void Reset()
    {
        _paramInjectSettings = null;
        _isInitialized = false;
    }
}
