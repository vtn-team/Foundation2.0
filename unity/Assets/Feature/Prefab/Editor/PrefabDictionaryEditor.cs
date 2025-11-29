using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// PrefabDictionaryのカスタムエディタ
/// </summary>
[CustomEditor(typeof(PrefabDictionary))]
public class PrefabDictionaryEditor : Editor
{
    private string _newKeyName = "";
    private GameObject _newPrefab;
    private int _newLimit = 10;
    private List<string> _validationErrors = new List<string>();

    private SerializedProperty _prefabDicListProperty;

    private void OnEnable()
    {
        _prefabDicListProperty = serializedObject.FindProperty("prefabDicList");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var prefabDictionary = (PrefabDictionary)target;

        // デフォルトのリスト表示
        EditorGUILayout.PropertyField(_prefabDicListProperty, true);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("新規追加", EditorStyles.boldLabel);

        // 追加用の入力フィールド
        _newKeyName = EditorGUILayout.TextField("Key Name", _newKeyName);
        _newPrefab = (GameObject)EditorGUILayout.ObjectField("Prefab", _newPrefab, typeof(GameObject), false);
        _newLimit = EditorGUILayout.IntField("Limit", _newLimit);

        EditorGUILayout.Space(5);

        // 追加ボタン
        if (GUILayout.Button("追加"))
        {
            if (ValidateNewEntry(prefabDictionary))
            {
                Undo.RecordObject(prefabDictionary, "Add Prefab Entry");

                var newItem = new PrefabDicItem
                {
                    keyName = _newKeyName,
                    prefab = _newPrefab,
                    limit = _newLimit
                };

                prefabDictionary.AddList(newItem);

                // フィールドをクリア
                _newKeyName = "";
                _newPrefab = null;
                _newLimit = 10;

                EditorUtility.SetDirty(prefabDictionary);
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "エラーを解決してください", "OK");
            }
        }

        EditorGUILayout.Space(5);

        // 保存ボタン
        if (GUILayout.Button("保存"))
        {
            if (ValidateAll(prefabDictionary))
            {
                EditorUtility.SetDirty(prefabDictionary);
                AssetDatabase.SaveAssets();
                prefabDictionary.RebuildDictionary();
                Debug.Log("[PrefabDictionary] 保存しました");
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "エラーを解決してください", "OK");
            }
        }

        // バリデーションエラー表示
        if (_validationErrors.Count > 0)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("バリデーションエラー", EditorStyles.boldLabel);

            foreach (var error in _validationErrors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 新規エントリのバリデーション
    /// </summary>
    private bool ValidateNewEntry(PrefabDictionary prefabDictionary)
    {
        _validationErrors.Clear();

        // 入力値の確認
        if (string.IsNullOrEmpty(_newKeyName))
        {
            _validationErrors.Add("Key Nameを入力してください");
        }

        if (_newPrefab == null)
        {
            _validationErrors.Add("Prefabを設定してください");
        }

        // 重複キーの確認
        if (!string.IsNullOrEmpty(_newKeyName) && prefabDictionary.IsKeyRegistered(_newKeyName))
        {
            _validationErrors.Add($"Key '{_newKeyName}' は既に登録されています");
        }

        return _validationErrors.Count == 0;
    }

    /// <summary>
    /// 全体のバリデーション
    /// </summary>
    private bool ValidateAll(PrefabDictionary prefabDictionary)
    {
        _validationErrors.Clear();

        var keySet = new HashSet<string>();
        var list = prefabDictionary.PrefabDicList;

        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];

            if (string.IsNullOrEmpty(item.keyName))
            {
                _validationErrors.Add($"インデックス {i}: Key Nameが空です");
                continue;
            }

            if (keySet.Contains(item.keyName))
            {
                _validationErrors.Add($"Key '{item.keyName}' が重複しています");
            }
            else
            {
                keySet.Add(item.keyName);
            }
        }

        return _validationErrors.Count == 0;
    }
}
