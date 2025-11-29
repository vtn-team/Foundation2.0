using UnityEngine;
using UnityEditor;

/// <summary>
/// Quantizerの状態を確認するエディタウィンドウ
/// </summary>
public class QuantizerDebugWindow : EditorWindow
{
    private Vector2 _scrollPosition;

    [MenuItem("Tools/Quantizer Debug")]
    public static void ShowWindow()
    {
        GetWindow<QuantizerDebugWindow>("Quantizer Debug");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Quantizer Status", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("プレイモードで実行してください", MessageType.Info);
            return;
        }

        var quantizer = Quantizer.Instance;

        // 基本情報
        EditorGUILayout.LabelField("基本情報", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.LabelField("実行中", quantizer.IsRunning ? "Yes" : "No");
        EditorGUILayout.LabelField("BPM", quantizer.BPM.ToString("F1"));
        EditorGUILayout.LabelField("1拍の長さ", $"{quantizer.BeatDuration:F3} 秒");
        EditorGUILayout.LabelField("1小節の長さ", $"{quantizer.MeasureDuration:F3} 秒");

        if (quantizer.IsRunning)
        {
            EditorGUILayout.LabelField("現在時間", $"{quantizer.CurrentTime:F3} 秒");
            EditorGUILayout.LabelField("現在の拍", $"{quantizer.CurrentBeat:F2}");
            EditorGUILayout.LabelField("次の小節まで", $"{quantizer.NextMeasureTime():F3} 秒");
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space();

        // 登録イベント
        EditorGUILayout.LabelField($"登録イベント ({quantizer.PendingEventCount}件)", EditorStyles.boldLabel);

        if (quantizer.PendingEventCount > 0)
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));

            foreach (var evt in quantizer.PendingEvents)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField("説明", evt.Description);
                EditorGUILayout.LabelField("拍分割", evt.BeatDivision > 0 ? evt.BeatDivision.ToString() : "-");
                EditorGUILayout.LabelField("実行予定時間", $"{evt.ScheduledTime:F3} 秒");

                float remaining = evt.ScheduledTime - (float)quantizer.CurrentTime;
                EditorGUILayout.LabelField("残り時間", $"{remaining:F3} 秒");

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("登録されているイベントはありません", MessageType.Info);
        }

        // 更新ボタン
        EditorGUILayout.Space();
        if (GUILayout.Button("更新"))
        {
            Repaint();
        }

        // 自動更新
        if (quantizer.IsRunning)
        {
            Repaint();
        }
    }

    private void OnInspectorUpdate()
    {
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}
