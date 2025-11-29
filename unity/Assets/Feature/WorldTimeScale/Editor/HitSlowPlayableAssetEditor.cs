using UnityEngine;
using UnityEditor;
using UnityEditor.Timeline;

/// <summary>
/// HitSlowPlayableAssetのカスタムエディター
/// </summary>
[CustomEditor(typeof(HitSlowPlayableAsset))]
public class HitSlowPlayableAssetEditor : Editor
{
    private SerializedProperty _slowDurationProp;
    private SerializedProperty _centerWeightProp;
    private SerializedProperty _centerTimeScaleProp;
    private SerializedProperty _centerHoldTimeProp;
    private SerializedProperty _easeProp;

    private void OnEnable()
    {
        _slowDurationProp = serializedObject.FindProperty("slowDuration");
        _centerWeightProp = serializedObject.FindProperty("centerWeight");
        _centerTimeScaleProp = serializedObject.FindProperty("centerTimeScale");
        _centerHoldTimeProp = serializedObject.FindProperty("centerHoldTime");
        _easeProp = serializedObject.FindProperty("ease");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("HitSlow Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.PropertyField(_slowDurationProp, new GUIContent("スロー時間（秒）"));
        EditorGUILayout.PropertyField(_centerWeightProp, new GUIContent("中心ウェイト"));
        EditorGUILayout.PropertyField(_centerTimeScaleProp, new GUIContent("中心タイムスケール"));
        EditorGUILayout.PropertyField(_centerHoldTimeProp, new GUIContent("中心静止時間"));
        EditorGUILayout.PropertyField(_easeProp, new GUIContent("イージング"));

        // 値の検証
        if (_slowDurationProp.floatValue < 0)
        {
            _slowDurationProp.floatValue = 0;
        }
        if (_centerHoldTimeProp.floatValue < 0)
        {
            _centerHoldTimeProp.floatValue = 0;
        }
        if (_centerHoldTimeProp.floatValue > _slowDurationProp.floatValue)
        {
            _centerHoldTimeProp.floatValue = _slowDurationProp.floatValue;
        }

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();

            // タイムラインの更新をトリガー
            TimelineEditor.Refresh(RefreshReason.ContentsModified);
        }

        // タイムスケールカーブのプレビュー
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("タイムスケールカーブ", EditorStyles.boldLabel);
        DrawTimeScaleCurvePreview();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "ヒットスローはイン→ループ→アウトの3フェーズで動作します。\n" +
            "・イン: 1.0 → 中心タイムスケール\n" +
            "・ループ: 中心タイムスケール（固定）\n" +
            "・アウト: 中心タイムスケール → 1.0",
            MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawTimeScaleCurvePreview()
    {
        Rect rect = GUILayoutUtility.GetRect(100, 60);
        rect = EditorGUI.IndentedRect(rect);

        if (Event.current.type == EventType.Repaint)
        {
            GUI.Box(rect, GUIContent.none);

            float duration = _slowDurationProp.floatValue;
            float holdTime = _centerHoldTimeProp.floatValue;
            float centerTimeScale = _centerTimeScaleProp.floatValue;

            if (duration <= 0) return;

            float transitionDuration = (duration - holdTime) * 0.5f;
            float centerStart = transitionDuration / duration;
            float centerEnd = (transitionDuration + holdTime) / duration;

            Handles.BeginGUI();

            // 背景グリッド
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            Handles.DrawLine(
                new Vector3(rect.x, rect.y + rect.height * 0.5f),
                new Vector3(rect.xMax, rect.y + rect.height * 0.5f)
            );

            // カーブを描画
            Handles.color = Color.cyan;
            Vector3 prevPoint = new Vector3(rect.x, rect.y + rect.height * (1f - 1f));

            int segments = 50;
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                float timeScale;

                if (t < centerStart)
                {
                    float progress = t / centerStart;
                    timeScale = Mathf.Lerp(1f, centerTimeScale, progress);
                }
                else if (t < centerEnd)
                {
                    timeScale = centerTimeScale;
                }
                else
                {
                    float progress = (t - centerEnd) / (1f - centerEnd);
                    timeScale = Mathf.Lerp(centerTimeScale, 1f, progress);
                }

                Vector3 point = new Vector3(
                    rect.x + rect.width * t,
                    rect.y + rect.height * (1f - timeScale)
                );

                Handles.DrawLine(prevPoint, point);
                prevPoint = point;
            }

            Handles.EndGUI();
        }
    }
}
