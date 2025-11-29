using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEditor;

/// <summary>
/// CueTimelineDirectorのカスタムエディター
/// </summary>
[CustomEditor(typeof(CueTimelineDirector))]
public class CueTimelineDirectorEditor : Editor
{
    private SerializedProperty _bpmProp;
    private SerializedProperty _ignoreTrackSettingsProp;

    private void OnEnable()
    {
        _bpmProp = serializedObject.FindProperty("bpm");
        _ignoreTrackSettingsProp = serializedObject.FindProperty("ignoreTrackSettings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var director = target as CueTimelineDirector;

        EditorGUILayout.LabelField("Cue Timeline Director", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // BPM設定
        EditorGUILayout.PropertyField(_bpmProp, new GUIContent("BPM"));
        EditorGUILayout.LabelField($"1拍: {director.BeatDuration:F3}秒");
        EditorGUILayout.Space();

        // 無視するトラック設定
        EditorGUILayout.PropertyField(_ignoreTrackSettingsProp, new GUIContent("無視するトラック設定"), true);
        EditorGUILayout.Space();

        // PlayableDirector参照
        var playableDirector = director.Director;
        if (playableDirector != null)
        {
            EditorGUILayout.LabelField("Timeline", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("PlayableDirector", playableDirector, typeof(PlayableDirector), true);
            EditorGUI.EndDisabledGroup();

            var timeline = playableDirector.playableAsset as TimelineAsset;
            if (timeline != null)
            {
                EditorGUILayout.LabelField($"Timeline: {timeline.name}");
                EditorGUILayout.LabelField($"Duration: {timeline.duration:F2}秒");
                EditorGUILayout.Space();

                // トラック一覧
                EditorGUILayout.LabelField("トラック一覧", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                foreach (var track in timeline.GetOutputTracks())
                {
                    bool isIgnored = director.IsIgnoredTrack(timeline.name, track.name);
                    string status = isIgnored ? " [無視]" : "";
                    EditorGUILayout.LabelField($"{track.GetType().Name}: {track.name}{status}");

                    // クリップの時間チェック
                    foreach (var clip in track.GetClips())
                    {
                        bool onBeat = director.IsOnBeat((float)clip.start);
                        if (!onBeat)
                        {
                            EditorGUILayout.HelpBox(
                                $"クリップ '{clip.displayName}' (開始: {clip.start:F3}秒) はビートに一致していません",
                                MessageType.Warning);
                        }
                    }
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                // シーケンス生成ボタン
                if (GUILayout.Button("シーケンスを保存", GUILayout.Height(30)))
                {
                    CueSequenceCodeBuilder.GenerateCueSequence(timeline, director);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("PlayableDirectorにTimelineAssetが設定されていません", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("PlayableDirectorコンポーネントが見つかりません", MessageType.Error);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
