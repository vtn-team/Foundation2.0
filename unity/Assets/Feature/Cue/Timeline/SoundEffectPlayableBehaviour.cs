using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// SEのPlayableBehaviour実装
/// </summary>
public class SoundEffectPlayableBehaviour : PlayableBehaviour
{
    public string SoundKey;
    public int QuantizeBeat = 16;

    private bool _hasPlayed;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!Application.isPlaying) return;

        if (!_hasPlayed && !string.IsNullOrEmpty(SoundKey))
        {
            _hasPlayed = true;
            SoundManager.Instance?.PlaySE(SoundKey);
        }
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        _hasPlayed = false;
    }
}
