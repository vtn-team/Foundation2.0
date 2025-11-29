using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// ParamInjectSettingsのカスタムエディタ
/// </summary>
[CustomEditor(typeof(ParamInjectSettings))]
public class InjectSelector : Editor
{
    private List<InjectParamList> _paramLists;
    private bool _needsRefresh = true;

    private void OnEnable()
    {
        RefreshParamLists();
    }

    public override void OnInspectorGUI()
    {
        // ゲーム再生中は何もしない
        if (Application.isPlaying)
        {
            EditorGUILayout.HelpBox("ゲーム再生中は編集できません", MessageType.Info);
            DrawDefaultInspector();
            return;
        }

        var settings = (ParamInjectSettings)target;

        serializedObject.Update();

        // デフォルトのプロパティ表示
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("InjectParamList 一覧", EditorStyles.boldLabel);

        // リフレッシュボタン
        if (GUILayout.Button("リストを更新"))
        {
            RefreshParamLists();
        }

        EditorGUILayout.Space(5);

        // InjectParamListのリストを表示
        if (_paramLists == null || _paramLists.Count == 0)
        {
            EditorGUILayout.HelpBox("InjectParamListが見つかりません", MessageType.Warning);
        }
        else
        {
            foreach (var paramList in _paramLists)
            {
                if (paramList == null) continue;

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                EditorGUILayout.BeginVertical();

                // リスト名と説明
                EditorGUILayout.LabelField(paramList.ListName, EditorStyles.boldLabel);
                if (!string.IsNullOrEmpty(paramList.Description))
                {
                    EditorGUILayout.LabelField(paramList.Description, EditorStyles.wordWrappedMiniLabel);
                }

                EditorGUILayout.EndVertical();

                // 選択中マーク
                if (settings.SelectedParamList == paramList)
                {
                    EditorGUILayout.LabelField("✓", GUILayout.Width(20));
                }

                // Selectボタン
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    SelectParamList(settings, paramList);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// プロジェクト内のInjectParamListを検索
    /// </summary>
    private void RefreshParamLists()
    {
        _paramLists = new List<InjectParamList>();

        var guids = AssetDatabase.FindAssets("t:InjectParamList");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var paramList = AssetDatabase.LoadAssetAtPath<InjectParamList>(path);
            if (paramList != null)
            {
                _paramLists.Add(paramList);
            }
        }

        _needsRefresh = false;
    }

    /// <summary>
    /// パラメータリストを選択
    /// </summary>
    private void SelectParamList(ParamInjectSettings settings, InjectParamList paramList)
    {
        Undo.RecordObject(settings, "Select InjectParamList");
        settings.SelectedParamList = paramList;
        EditorUtility.SetDirty(settings);
    }
}
