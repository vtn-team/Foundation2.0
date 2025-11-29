using UnityEngine;
using UnityEngine.Playables;
using DG.Tweening;

/// <summary>
/// ヒットスローのPlayableBehaviour実装
/// </summary>
public class HitSlowPlayableBehaviour : PlayableBehaviour
{
    public float SlowDuration;
    public float CenterWeight;
    public float CenterTimeScale;
    public float CenterHoldTime;
    public Ease EaseType;

    private bool _hasStarted;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!Application.isPlaying) return;

        if (!_hasStarted)
        {
            _hasStarted = true;
            WorldTimeComposer.Instance.HitSlow(SlowDuration, CenterWeight, CenterTimeScale, CenterHoldTime, EaseType, false);
        }
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        _hasStarted = false;
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        // エディタプレビュー用の処理はMixerで行う
    }
}
