using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

/// <summary>
/// InjectParamListの動的メニュー生成を管理
/// </summary>
[InitializeOnLoad]
public static class InjectMenuGenerator
{
    private const string GENERATED_PATH = "Assets/Feature/Inject/Editor/Generated/";
    private const string MENU_FILE = "InjectParamListMenu.Generated.cs";
    private const string PARAM_INJECT_SETTINGS_PATH = "Assets/DataAsset/Params/ParamInjectSettings.asset";

    static InjectMenuGenerator()
    {
        // Unity起動時に自動的にメニューを生成
        EditorApplication.delayCall += GenerateMenu;
    }

    /// <summary>
    /// メニューから手動実行
    /// </summary>
    [MenuItem("Tools/Inject/Generate Menu")]
    public static void GenerateMenu()
    {
        var paramLists = FindAllInjectParamLists();

        if (paramLists.Count == 0)
        {
            Debug.Log("[InjectMenuGenerator] InjectParamListが見つかりませんでした");
            return;
        }

        // 出力ディレクトリを作成
        if (!Directory.Exists(GENERATED_PATH))
        {
            Directory.CreateDirectory(GENERATED_PATH);
        }

        GenerateMenuCode(paramLists);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// プロジェクト内のInjectParamListを検索
    /// </summary>
    private static List<(string path, InjectParamList paramList)> FindAllInjectParamLists()
    {
        var result = new List<(string, InjectParamList)>();

        var guids = AssetDatabase.FindAssets("t:InjectParamList");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var paramList = AssetDatabase.LoadAssetAtPath<InjectParamList>(path);
            if (paramList != null)
            {
                result.Add((path, paramList));
            }
        }

        return result;
    }

    /// <summary>
    /// メニューコードを生成
    /// </summary>
    private static void GenerateMenuCode(List<(string path, InjectParamList paramList)> paramLists)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// このファイルは自動生成されています。編集しないでください。");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using UnityEditor;");
        sb.AppendLine();
        sb.AppendLine("public static partial class InjectParamListMenu");
        sb.AppendLine("{");

        int index = 0;
        foreach (var (path, paramList) in paramLists)
        {
            var menuName = string.IsNullOrEmpty(paramList.ListName) ? $"ParamList_{index}" : paramList.ListName;
            var methodName = $"SelectParamList_{index}";

            sb.AppendLine($"    [MenuItem(\"Tools/Inject/Select Param List/{menuName}\")]");
            sb.AppendLine($"    private static void {methodName}()");
            sb.AppendLine("    {");
            sb.AppendLine($"        SelectParamListByPath(\"{path}\");");
            sb.AppendLine("    }");
            sb.AppendLine();

            index++;
        }

        sb.AppendLine("}");

        var filePath = Path.Combine(GENERATED_PATH, MENU_FILE);
        var newContent = sb.ToString();

        if (ShouldWriteFile(filePath, newContent))
        {
            File.WriteAllText(filePath, newContent);
            Debug.Log($"[InjectMenuGenerator] メニュー生成完了: {paramLists.Count}個のInjectParamList");
        }
    }

    /// <summary>
    /// ファイルを書き込むべきか判定
    /// </summary>
    private static bool ShouldWriteFile(string filePath, string newContent)
    {
        if (!File.Exists(filePath))
        {
            return true;
        }

        var existingContent = File.ReadAllText(filePath);
        return existingContent != newContent;
    }

    /// <summary>
    /// パスからInjectParamListを選択
    /// </summary>
    public static void SelectParamListByPath(string path)
    {
        var settings = AssetDatabase.LoadAssetAtPath<ParamInjectSettings>(PARAM_INJECT_SETTINGS_PATH);
        if (settings == null)
        {
            Debug.LogError($"[InjectMenuGenerator] ParamInjectSettingsが見つかりません: {PARAM_INJECT_SETTINGS_PATH}");
            return;
        }

        var paramList = AssetDatabase.LoadAssetAtPath<InjectParamList>(path);
        if (paramList == null)
        {
            Debug.LogError($"[InjectMenuGenerator] InjectParamListが見つかりません: {path}");
            return;
        }

        Undo.RecordObject(settings, "Select InjectParamList");
        settings.SelectedParamList = paramList;
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        Debug.Log($"[InjectMenuGenerator] '{paramList.ListName}' を選択しました");
    }
}

/// <summary>
/// InjectParamListMenuのpartial class（手動作成部分）
/// </summary>
public static partial class InjectParamListMenu
{
    // 自動生成されるメソッドと結合される
}
