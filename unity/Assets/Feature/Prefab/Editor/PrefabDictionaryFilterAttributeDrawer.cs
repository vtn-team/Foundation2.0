using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// PrefabDictionaryFilterAttributeのPropertyDrawer
/// </summary>
[CustomPropertyDrawer(typeof(PrefabDictionaryFilterAttribute))]
public class PrefabDictionaryFilterAttributeDrawer : PropertyDrawer
{
    private const string PREFAB_DICTIONARY_PATH = "Assets/DataAsset/PrefabDictionary.asset";

    private PrefabDictionary _cachedDictionary;
    private string[] _filteredKeys;
    private bool _needsRefresh = true;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var filterAttr = (PrefabDictionaryFilterAttribute)attribute;

        // PrefabDictionaryを取得
        if (_cachedDictionary == null || _needsRefresh)
        {
            RefreshFilteredKeys(filterAttr);
        }

        if (_filteredKeys == null || _filteredKeys.Length == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        // 現在の値のインデックスを取得
        int currentIndex = 0;
        string currentValue = property.stringValue;

        for (int i = 0; i < _filteredKeys.Length; i++)
        {
            if (_filteredKeys[i] == currentValue)
            {
                currentIndex = i;
                break;
            }
        }

        // ポップアップを表示
        EditorGUI.BeginProperty(position, label, property);

        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, _filteredKeys);
        if (newIndex != currentIndex || string.IsNullOrEmpty(currentValue))
        {
            property.stringValue = _filteredKeys[newIndex];
        }

        EditorGUI.EndProperty();

        // 右クリックメニュー
        if (Event.current.type == EventType.ContextClick && position.Contains(Event.current.mousePosition))
        {
            ShowContextMenu(property, filterAttr);
            Event.current.Use();
        }
    }

    /// <summary>
    /// フィルタリングされたキーリストを更新
    /// </summary>
    private void RefreshFilteredKeys(PrefabDictionaryFilterAttribute filterAttr)
    {
        _cachedDictionary = AssetDatabase.LoadAssetAtPath<PrefabDictionary>(PREFAB_DICTIONARY_PATH);

        if (_cachedDictionary == null)
        {
            _filteredKeys = new string[0];
            return;
        }

        var keys = new List<string>();

        // 空の選択肢を追加
        if (filterAttr.IncludeEmpty)
        {
            keys.Add("");
        }

        var allKeys = _cachedDictionary.GetKeyList();
        foreach (var key in allKeys)
        {
            // プレフィックスフィルタ
            if (!string.IsNullOrEmpty(filterAttr.KeyPrefix) && !key.StartsWith(filterAttr.KeyPrefix))
            {
                continue;
            }

            // コンポーネントフィルタ
            var prefab = _cachedDictionary.GetPrefab(key);
            if (prefab == null) continue;

            if (filterAttr.ComponentType != null)
            {
                // インターフェースの場合
                if (filterAttr.ComponentType.IsInterface)
                {
                    if (prefab.GetComponent(filterAttr.ComponentType) == null)
                    {
                        continue;
                    }
                }
                else
                {
                    // コンポーネントの場合
                    if (prefab.GetComponent(filterAttr.ComponentType) == null)
                    {
                        continue;
                    }
                }
            }

            keys.Add(key);
        }

        _filteredKeys = keys.ToArray();
        _needsRefresh = false;
    }

    /// <summary>
    /// 右クリックメニューを表示
    /// </summary>
    private void ShowContextMenu(SerializedProperty property, PrefabDictionaryFilterAttribute filterAttr)
    {
        var menu = new GenericMenu();

        menu.AddItem(new GUIContent("Refresh List"), false, () =>
        {
            _needsRefresh = true;
        });

        menu.AddItem(new GUIContent("Select PrefabDictionary"), false, () =>
        {
            var dict = AssetDatabase.LoadAssetAtPath<PrefabDictionary>(PREFAB_DICTIONARY_PATH);
            if (dict != null)
            {
                Selection.activeObject = dict;
            }
        });

        if (!string.IsNullOrEmpty(property.stringValue) && _cachedDictionary != null)
        {
            menu.AddItem(new GUIContent("Select Prefab"), false, () =>
            {
                var prefab = _cachedDictionary.GetPrefab(property.stringValue);
                if (prefab != null)
                {
                    Selection.activeObject = prefab;
                }
            });
        }
        else
        {
            menu.AddDisabledItem(new GUIContent("Select Prefab"));
        }

        menu.ShowAsContext();
    }
}
