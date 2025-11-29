using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// SEのトラック実装
/// </summary>
[TrackColor(0.2f, 0.6f, 0.9f)]
[TrackClipType(typeof(SoundEffectPlayableAsset))]
public class SoundEffectTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return base.CreateTrackMixer(graph, go, inputCount);
    }
}
