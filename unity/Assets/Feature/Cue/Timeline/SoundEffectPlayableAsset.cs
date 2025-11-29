using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// SEのPlayableAsset実装
/// </summary>
[System.Serializable]
public class SoundEffectPlayableAsset : PlayableAsset, ITimelineClipAsset
{
    [SerializeField]
    [Tooltip("再生するSEのキー")]
    private string soundKey;

    [SerializeField]
    [Tooltip("クォンタイズ用のビート")]
    private int quantizeBeat = 16;

    /// <summary>
    /// SEキー
    /// </summary>
    public string SoundKey
    {
        get => soundKey;
        set => soundKey = value;
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
        var playable = ScriptPlayable<SoundEffectPlayableBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.SoundKey = soundKey;
        behaviour.QuantizeBeat = quantizeBeat;
        return playable;
    }
}
