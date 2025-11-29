using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// EffectPlayerのPlayableBehaviour実装
/// </summary>
public class EffectPlayerPlayableBehaviour : PlayableBehaviour
{
    public string PrefabKey;
    public int QuantizeBeat = 16;

    private BasicEffect _effect;
    private bool _hasPlayed;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!Application.isPlaying) return;

        if (!_hasPlayed && !string.IsNullOrEmpty(PrefabKey))
        {
            _hasPlayed = true;
            PlayEffect();
        }
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        _hasPlayed = false;
        StopEffect();
    }

    private void PlayEffect()
    {
        var instance = PrefabStock.CreateInstance(PrefabKey);
        if (instance == null) return;

        _effect = instance.GetComponent<BasicEffect>();
        if (_effect != null)
        {
            _effect.Play();
        }
    }

    private void StopEffect()
    {
        if (_effect != null)
        {
            _effect.Stop();
            _effect = null;
        }
    }
}
