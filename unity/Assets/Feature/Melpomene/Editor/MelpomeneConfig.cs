#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Melpomene
{
    /// <summary>
    /// Melpomene設定アセット
    /// NOTE: GitHub接続情報やデフォルト設定を保持
    /// </summary>
    [CreateAssetMenu(fileName = "MelpomeneConfig", menuName = "Melpomene/Config")]
    public class MelpomeneConfig : ScriptableObject
    {
        /// <summary>
        /// Melpomeneのバージョン
        /// NOTE: Issue互換性のために使用
        /// </summary>
        public const string Version = "1.0.0";

        private const string CONFIG_PATH = "Assets/Foundation/Scripts/BaseSystemEditor/Editor/Melpomene/MelpomeneConfig.asset";

        [Header("GitHub Repository")]
        [Tooltip("リポジトリオーナー（組織名またはユーザー名）")]
        public string repositoryOwner = "";

        [Tooltip("リポジトリ名")]
        public string repositoryName = "";

        [Header("GitHub Authentication")]
        [Tooltip("GitHub Personal Access Token（Issues書き込み権限が必要）")]
        public string accessToken = "";

        [Header("Default Settings")]
        [Tooltip("デフォルトで付与するラベル")]
        public string[] defaultLabels = new string[] { "melpomene", "auto-generated" };

        [Tooltip("デフォルトの優先度")]
        public MelpomenePriority defaultPriority = MelpomenePriority.Medium;

        [Tooltip("デフォルトのカテゴリ")]
        public MelpomeneCategory defaultCategory = MelpomeneCategory.Bug;

        [Header("Cache Settings")]
        [Tooltip("キャッシュ有効期限（分）")]
        public int cacheDurationMinutes = 10;

        [Header("User Settings")]
        [Tooltip("デフォルトのユーザー名")]
        public string defaultUserName = "";

        [Header("Shortcut Settings")]
        [Tooltip("Alt+クリックでチケット作成を有効にする")]
        public bool enableAltClickShortcut = true;

        /// <summary>
        /// GitHub API URL
        /// </summary>
        public string ApiBaseUrl => $"https://api.github.com/repos/{repositoryOwner}/{repositoryName}";

        /// <summary>
        /// 設定が有効かどうか
        /// </summary>
        public bool IsValid =>
            !string.IsNullOrEmpty(repositoryOwner) &&
            !string.IsNullOrEmpty(repositoryName) &&
            !string.IsNullOrEmpty(accessToken);

        /// <summary>
        /// 設定アセットを取得または作成
        /// </summary>
        public static MelpomeneConfig GetOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<MelpomeneConfig>(CONFIG_PATH);

            if (config == null)
            {
                // 設定ファイルが存在しない場合は作成
                config = CreateInstance<MelpomeneConfig>();

                // ディレクトリ確認
                var directory = System.IO.Path.GetDirectoryName(CONFIG_PATH);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                AssetDatabase.CreateAsset(config, CONFIG_PATH);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Melpomene] Created config at: {CONFIG_PATH}");
            }

            return config;
        }

        /// <summary>
        /// 設定ウィンドウを開く
        /// </summary>
        [MenuItem("Tools/Melpomene/Settings")]
        public static void OpenSettings()
        {
            var config = GetOrCreateConfig();
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
    }
}
#endif
