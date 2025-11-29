using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// EffectPlayerのトラック実装
/// </summary>
[TrackColor(0.4f, 0.9f, 0.5f)]
[TrackClipType(typeof(EffectPlayerPlayableAsset))]
public class EffectPlayerTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return base.CreateTrackMixer(graph, go, inputCount);
    }
}
