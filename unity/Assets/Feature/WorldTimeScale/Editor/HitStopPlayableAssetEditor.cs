using UnityEngine;
using UnityEditor;
using UnityEditor.Timeline;

/// <summary>
/// HitStopPlayableAssetのカスタムエディター
/// </summary>
[CustomEditor(typeof(HitStopPlayableAsset))]
public class HitStopPlayableAssetEditor : Editor
{
    private SerializedProperty _stopDurationProp;

    private void OnEnable()
    {
        _stopDurationProp = serializedObject.FindProperty("stopDuration");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("HitStop Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(_stopDurationProp, new GUIContent("停止時間（秒）"));

        if (_stopDurationProp.floatValue < 0)
        {
            _stopDurationProp.floatValue = 0;
        }

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();

            // タイムラインの更新をトリガー
            TimelineEditor.Refresh(RefreshReason.ContentsModified);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "ヒットストップ中は全てのアニメーションとエフェクトが停止します。\n" +
            "クリップの長さは自動的に停止時間に合わせて調整されます。",
            MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}
