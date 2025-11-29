using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Voiceのトラック実装
/// </summary>
[TrackColor(0.9f, 0.7f, 0.2f)]
[TrackClipType(typeof(VoicePlayableAsset))]
public class VoiceTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return base.CreateTrackMixer(graph, go, inputCount);
    }
}
