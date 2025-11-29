using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// ヒットストップのトラック実装（赤色トラック）
/// </summary>
[TrackColor(0.8f, 0.2f, 0.2f)]
[TrackClipType(typeof(HitStopPlayableAsset))]
public class HitStopTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
#if UNITY_EDITOR
        return ScriptPlayable<HitStopTrackMixer>.Create(graph, inputCount);
#else
        return base.CreateTrackMixer(graph, go, inputCount);
#endif
    }
}
