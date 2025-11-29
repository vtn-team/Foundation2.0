using UnityEngine;
using UnityEngine.Playables;

#if UNITY_EDITOR
/// <summary>
/// ヒットストップトラックのミキサー実装（Editor専用）
/// 他のトラックに自動的に影響を与える
/// </summary>
public class HitStopTrackMixer : PlayableBehaviour
{
    private PlayableDirector _director;
    private bool _isInHitStop;

    public override void OnPlayableCreate(Playable playable)
    {
        var graph = playable.GetGraph();
        var resolver = graph.GetResolver();
        if (resolver is PlayableDirector director)
        {
            _director = director;
        }
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        int inputCount = playable.GetInputCount();
        bool isActive = false;

        // アクティブなクリップがあるか確認
        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight > 0)
            {
                isActive = true;
                break;
            }
        }

        if (isActive != _isInHitStop)
        {
            _isInHitStop = isActive;
            float timeScale = isActive ? 0f : 1f;
            ApplyTimeScaleToTracks(timeScale);
        }
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        // Graph停止時にspeed=1にリセット
        ApplyTimeScaleToTracks(1f);
    }

    private void ApplyTimeScaleToTracks(float timeScale)
    {
        if (_director == null) return;

        // AnimationTrackのAnimator.speedを制御
        var animators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
        foreach (var animator in animators)
        {
            if (animator != null)
            {
                animator.speed = timeScale;
            }
        }

        // ParticleSystemのsimulationSpeedを制御
        var particles = Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (var ps in particles)
        {
            if (ps != null)
            {
                var main = ps.main;
                main.simulationSpeed = timeScale;
            }
        }
    }
}
#endif
