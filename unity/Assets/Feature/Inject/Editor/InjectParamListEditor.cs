using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

/// <summary>
/// InjectParamListのカスタムエディタ
/// InjectParamInfoを参照して表示を成形する
/// </summary>
[CustomEditor(typeof(InjectParamList), true)]
public class InjectParamListEditor : Editor
{
    private const string INJECT_PARAM_INFO_PATH = "Assets/DataAsset/Params/InjectParamInfo.asset";

    private InjectParamInfo _paramInfo;
    private Dictionary<string, bool> _groupFoldouts = new Dictionary<string, bool>();

    private void OnEnable()
    {
        // InjectParamInfoを読み込む
        _paramInfo = AssetDatabase.LoadAssetAtPath<InjectParamInfo>(INJECT_PARAM_INFO_PATH);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var paramList = (InjectParamList)target;

        // 基本情報
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_listName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_description"));

        EditorGUILayout.Space(10);

        // InjectParamInfoがない場合は通常表示
        if (_paramInfo == null)
        {
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.LabelField("注入パラメータ", EditorStyles.boldLabel);

        // グループごとに分類して表示
        var groups = _paramInfo.GetAllGroups();
        var displayedProperties = new HashSet<string>();

        foreach (var group in groups)
        {
            if (!_groupFoldouts.ContainsKey(group))
            {
                _groupFoldouts[group] = true;
            }

            _groupFoldouts[group] = EditorGUILayout.Foldout(_groupFoldouts[group], group, true);

            if (_groupFoldouts[group])
            {
                EditorGUI.indentLevel++;

                var paramInfos = _paramInfo.GetByGroup(group);
                foreach (var info in paramInfos)
                {
                    var fieldName = "_" + info.VarName.TrimStart('_');
                    var property = serializedObject.FindProperty(fieldName);

                    if (property != null)
                    {
                        // 日本語表示名を使用
                        EditorGUILayout.PropertyField(property, new GUIContent(info.ViewName));
                        displayedProperties.Add(fieldName);
                    }
                    else
                    {
                        // フィールドが見つからない場合は警告
                        EditorGUILayout.HelpBox($"フィールドが見つかりません: {info.VarName}", MessageType.Warning);
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        // グループに属さないプロパティを表示
        EditorGUILayout.Space(5);

        var iterator = serializedObject.GetIterator();
        bool hasUngrouped = false;

        if (iterator.NextVisible(true))
        {
            do
            {
                // スキップするプロパティ
                if (iterator.name == "m_Script" ||
                    iterator.name == "_listName" ||
                    iterator.name == "_description")
                {
                    continue;
                }

                if (!displayedProperties.Contains(iterator.name))
                {
                    if (!hasUngrouped)
                    {
                        EditorGUILayout.LabelField("その他", EditorStyles.boldLabel);
                        hasUngrouped = true;
                    }

                    // InjectParamInfoから表示名を取得
                    var varName = iterator.name.TrimStart('_');
                    var viewName = _paramInfo.GetViewName(varName);
                    EditorGUILayout.PropertyField(iterator, new GUIContent(viewName));
                }
            }
            while (iterator.NextVisible(false));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
