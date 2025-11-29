using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// ヒットストップのPlayableBehaviour実装
/// </summary>
public class HitStopPlayableBehaviour : PlayableBehaviour
{
    public float StopDuration;

    private bool _hasStarted;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!Application.isPlaying) return;

        if (!_hasStarted)
        {
            _hasStarted = true;
            WorldTimeComposer.Instance.HitStop(StopDuration, false);
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
