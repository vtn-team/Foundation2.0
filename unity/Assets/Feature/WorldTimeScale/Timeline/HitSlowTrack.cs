using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// ヒットスローのトラック実装（ピンク色トラック）
/// </summary>
[TrackColor(0.9f, 0.4f, 0.6f)]
[TrackClipType(typeof(HitSlowPlayableAsset))]
public class HitSlowTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
#if UNITY_EDITOR
        return ScriptPlayable<HitSlowTrackMixer>.Create(graph, inputCount);
#else
        return base.CreateTrackMixer(graph, go, inputCount);
#endif
    }
}
