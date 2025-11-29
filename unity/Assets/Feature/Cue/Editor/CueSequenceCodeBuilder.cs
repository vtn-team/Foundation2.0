using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor;

/// <summary>
/// CueSequence派生クラスを自動生成するエディタツール
/// </summary>
public static class CueSequenceCodeBuilder
{
    private const string OutputPath = "Assets/Scripts/InGame/GeneratedCue/";

    /// <summary>
    /// タイムラインからCueSequenceを生成
    /// </summary>
    /// <param name="timeline">解析するタイムライン</param>
    /// <param name="director">CueTimelineDirector参照</param>
    public static void GenerateCueSequence(TimelineAsset timeline, CueTimelineDirector director)
    {
        if (timeline == null)
        {
            Debug.LogError("Timeline asset is null");
            return;
        }

        string className = timeline.name + "CueSequence";
        var builder = new StringBuilder();
        var fields = new List<string>();
        var setupParams = new List<string>();
        var setupAssignments = new List<string>();
        var trackMethods = new List<TrackMethodInfo>();

        // ヘッダー
        builder.AppendLine("using UnityEngine;");
        builder.AppendLine("using Cysharp.Threading.Tasks;");
        builder.AppendLine();
        builder.AppendLine($"/// <summary>");
        builder.AppendLine($"/// {timeline.name}から自動生成されたCueSequence");
        builder.AppendLine($"/// </summary>");
        builder.AppendLine($"public class {className} : CueSequence");
        builder.AppendLine("{");

        // トラックを解析
        foreach (var track in timeline.GetOutputTracks())
        {
            if (director != null && director.IsIgnoredTrack(timeline.name, track.name))
            {
                continue;
            }

            AnalyzeTrack(track, fields, setupParams, setupAssignments, trackMethods, timeline.name);
        }

        // フィールド宣言
        foreach (var field in fields)
        {
            builder.AppendLine($"    {field}");
        }

        if (fields.Count > 0)
        {
            builder.AppendLine();
        }

        // Setup関数
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// 依存関係の設定");
        builder.AppendLine("    /// </summary>");
        builder.Append("    public void Setup(");
        builder.Append(string.Join(", ", setupParams));
        builder.AppendLine(")");
        builder.AppendLine("    {");
        foreach (var assignment in setupAssignments)
        {
            builder.AppendLine($"        {assignment}");
        }
        builder.AppendLine("    }");
        builder.AppendLine();

        // PlayAsync関数
        builder.AppendLine("    /// <summary>");
        builder.AppendLine("    /// シーケンスを再生");
        builder.AppendLine("    /// </summary>");
        builder.AppendLine("    public override async UniTask PlayAsync()");
        builder.AppendLine("    {");
        builder.AppendLine("        IsPlaying = true;");
        builder.AppendLine();
        builder.AppendLine("        // 全トラックを並列実行");
        builder.AppendLine("        await UniTask.WhenAll(");
        for (int i = 0; i < trackMethods.Count; i++)
        {
            string comma = i < trackMethods.Count - 1 ? "," : "";
            builder.AppendLine($"            {trackMethods[i].MethodName}Async(){comma}");
        }
        builder.AppendLine("        );");
        builder.AppendLine();
        builder.AppendLine("        IsPlaying = false;");
        builder.AppendLine("    }");

        // 各トラックのメソッド
        foreach (var trackMethod in trackMethods)
        {
            builder.AppendLine();
            builder.AppendLine($"    private async UniTask {trackMethod.MethodName}Async()");
            builder.AppendLine("    {");
            foreach (var action in trackMethod.Actions)
            {
                builder.AppendLine($"        {action}");
            }
            builder.AppendLine("    }");
        }

        builder.AppendLine("}");

        // ファイル出力
        if (!Directory.Exists(OutputPath))
        {
            Directory.CreateDirectory(OutputPath);
        }

        string filePath = OutputPath + className + ".cs";
        File.WriteAllText(filePath, builder.ToString());
        AssetDatabase.Refresh();

        Debug.Log($"Generated: {filePath}");
    }

    private class TrackMethodInfo
    {
        public string MethodName;
        public List<string> Actions = new List<string>();
    }

    private static void AnalyzeTrack(
        TrackAsset track,
        List<string> fields,
        List<string> setupParams,
        List<string> setupAssignments,
        List<TrackMethodInfo> trackMethods,
        string timelineName)
    {
        string trackName = SanitizeName(track.name);
        var methodInfo = new TrackMethodInfo { MethodName = trackName };

        switch (track)
        {
            case AnimationTrack animTrack:
                AnalyzeAnimationTrack(animTrack, trackName, fields, setupParams, setupAssignments, methodInfo);
                break;

            case SoundEffectTrack seTrack:
                AnalyzeSoundEffectTrack(seTrack, trackName, fields, methodInfo);
                break;

            case VoiceTrack voiceTrack:
                AnalyzeVoiceTrack(voiceTrack, trackName, fields, methodInfo);
                break;

            case EffectPlayerTrack effectTrack:
                AnalyzeEffectPlayerTrack(effectTrack, trackName, fields, setupParams, setupAssignments, methodInfo);
                break;

            case HitStopTrack hitStopTrack:
                AnalyzeHitStopTrack(hitStopTrack, trackName, methodInfo);
                break;

            case HitSlowTrack hitSlowTrack:
                AnalyzeHitSlowTrack(hitSlowTrack, trackName, methodInfo);
                break;
        }

        if (methodInfo.Actions.Count > 0)
        {
            trackMethods.Add(methodInfo);
        }
    }

    private static void AnalyzeAnimationTrack(
        AnimationTrack track,
        string trackName,
        List<string> fields,
        List<string> setupParams,
        List<string> setupAssignments,
        TrackMethodInfo methodInfo)
    {
        // TODO: MotionController検出を実装
        string fieldName = $"_animator{trackName}";
        fields.Add($"private Animator {fieldName};");
        setupParams.Add($"Animator animator{trackName}");
        setupAssignments.Add($"{fieldName} = animator{trackName};");

        foreach (var clip in track.GetClips())
        {
            float startTime = (float)clip.start;
            string clipName = clip.displayName;
            methodInfo.Actions.Add($"await Quantizer.Instance.QuantizeTimer(0, {startTime}f);");
            methodInfo.Actions.Add($"{fieldName}?.Play(\"{clipName}\");");
        }
    }

    private static void AnalyzeSoundEffectTrack(
        SoundEffectTrack track,
        string trackName,
        List<string> fields,
        TrackMethodInfo methodInfo)
    {
        string fieldName = $"_soundPlayer{trackName}";
        fields.Add($"private SoundPlayer {fieldName} = new SoundPlayer(SoundType.SE);");

        foreach (var clip in track.GetClips())
        {
            var asset = clip.asset as SoundEffectPlayableAsset;
            if (asset == null) continue;

            float startTime = (float)clip.start;
            string soundKey = asset.SoundKey;
            methodInfo.Actions.Add($"await Quantizer.Instance.QuantizeTimer(0, {startTime}f);");
            methodInfo.Actions.Add($"{fieldName}.Play(\"{soundKey}\");");
        }
    }

    private static void AnalyzeVoiceTrack(
        VoiceTrack track,
        string trackName,
        List<string> fields,
        TrackMethodInfo methodInfo)
    {
        string fieldName = $"_voicePlayer{trackName}";
        fields.Add($"private SoundPlayer {fieldName} = new SoundPlayer(SoundType.Voice);");

        foreach (var clip in track.GetClips())
        {
            var asset = clip.asset as VoicePlayableAsset;
            if (asset == null) continue;

            float startTime = (float)clip.start;
            string voiceKey = asset.VoiceKey;
            methodInfo.Actions.Add($"await Quantizer.Instance.QuantizeTimer(0, {startTime}f);");
            methodInfo.Actions.Add($"{fieldName}.Play(\"{voiceKey}\");");
        }
    }

    private static void AnalyzeEffectPlayerTrack(
        EffectPlayerTrack track,
        string trackName,
        List<string> fields,
        List<string> setupParams,
        List<string> setupAssignments,
        TrackMethodInfo methodInfo)
    {
        foreach (var clip in track.GetClips())
        {
            var asset = clip.asset as EffectPlayerPlayableAsset;
            if (asset == null) continue;

            string prefabKey = asset.PrefabKey;
            string safePrefabName = SanitizeName(prefabKey);
            string fieldName = $"_effectPlayer{safePrefabName}";

            if (!fields.Exists(f => f.Contains(fieldName)))
            {
                fields.Add($"private EffectPlayer {fieldName} = new EffectPlayer();");

                if (!string.IsNullOrEmpty(prefabKey))
                {
                    setupAssignments.Add($"{fieldName}.PrefabKey = \"{prefabKey}\";");
                }
            }

            float startTime = (float)clip.start;
            methodInfo.Actions.Add($"await Quantizer.Instance.QuantizeTimer(0, {startTime}f);");
            methodInfo.Actions.Add($"{fieldName}.Play();");
        }
    }

    private static void AnalyzeHitStopTrack(
        HitStopTrack track,
        string trackName,
        TrackMethodInfo methodInfo)
    {
        foreach (var clip in track.GetClips())
        {
            var asset = clip.asset as HitStopPlayableAsset;
            if (asset == null) continue;

            float startTime = (float)clip.start;
            float duration = asset.StopDuration;
            methodInfo.Actions.Add($"await Quantizer.Instance.QuantizeTimer(0, {startTime}f);");
            methodInfo.Actions.Add($"WorldTimeComposer.Instance.HitStop({duration}f);");
        }
    }

    private static void AnalyzeHitSlowTrack(
        HitSlowTrack track,
        string trackName,
        TrackMethodInfo methodInfo)
    {
        foreach (var clip in track.GetClips())
        {
            var asset = clip.asset as HitSlowPlayableAsset;
            if (asset == null) continue;

            float startTime = (float)clip.start;
            methodInfo.Actions.Add($"await Quantizer.Instance.QuantizeTimer(0, {startTime}f);");
            methodInfo.Actions.Add($"WorldTimeComposer.Instance.HitSlow({asset.SlowDuration}f, {asset.CenterWeight}f, {asset.CenterTimeScale}f, {asset.CenterHoldTime}f, DG.Tweening.Ease.{asset.EaseType});");
        }
    }

    private static string SanitizeName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unnamed";

        var sb = new StringBuilder();
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                sb.Append(c);
            }
        }

        string result = sb.ToString();
        if (result.Length > 0 && char.IsDigit(result[0]))
        {
            result = "_" + result;
        }

        return string.IsNullOrEmpty(result) ? "Unnamed" : result;
    }
}
