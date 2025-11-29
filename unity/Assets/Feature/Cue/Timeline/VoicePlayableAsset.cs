using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// VoiceのPlayableAsset実装
/// </summary>
[System.Serializable]
public class VoicePlayableAsset : PlayableAsset, ITimelineClipAsset
{
    [SerializeField]
    [Tooltip("再生するVoiceのキー")]
    private string voiceKey;

    [SerializeField]
    [Tooltip("クォンタイズ用のビート")]
    private int quantizeBeat = 16;

    /// <summary>
    /// Voiceキー
    /// </summary>
    public string VoiceKey
    {
        get => voiceKey;
        set => voiceKey = value;
    }

    /// <summary>
    /// クォンタイズビート
    /// </summary>
    public int QuantizeBeat
    {
        get => quantizeBeat;
        set => quantizeBeat = value;
    }

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<VoicePlayableBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.VoiceKey = voiceKey;
        behaviour.QuantizeBeat = quantizeBeat;
        return playable;
    }
}
