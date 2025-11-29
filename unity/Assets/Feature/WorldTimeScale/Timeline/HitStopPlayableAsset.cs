using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// ヒットストップのPlayableAsset実装
/// </summary>
[System.Serializable]
public class HitStopPlayableAsset : PlayableAsset, ITimelineClipAsset
{
    [SerializeField]
    [Tooltip("ヒットストップの時間（秒）")]
    private float stopDuration = 0.1f;

    /// <summary>
    /// ヒットストップの時間
    /// </summary>
    public float StopDuration
    {
        get => stopDuration;
        set => stopDuration = value;
    }

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<HitStopPlayableBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.StopDuration = stopDuration;
        return playable;
    }

    public override double duration => stopDuration;
}
