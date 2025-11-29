using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// VoiceのPlayableBehaviour実装
/// </summary>
public class VoicePlayableBehaviour : PlayableBehaviour
{
    public string VoiceKey;
    public int QuantizeBeat = 16;

    private bool _hasPlayed;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!Application.isPlaying) return;

        if (!_hasPlayed && !string.IsNullOrEmpty(VoiceKey))
        {
            _hasPlayed = true;
            SoundManager.Instance?.PlayVoice(VoiceKey);
        }
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        _hasPlayed = false;
    }
}
