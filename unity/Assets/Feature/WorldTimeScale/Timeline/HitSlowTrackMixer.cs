using UnityEngine;
using UnityEngine.Playables;
using DG.Tweening;

#if UNITY_EDITOR
/// <summary>
/// ヒットスロートラックのミキサー実装（Editor専用）
/// 他のトラックに自動的に影響を与える
/// </summary>
public class HitSlowTrackMixer : PlayableBehaviour
{
    private PlayableDirector _director;
    private float _currentTimeScale = 1f;

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
        float blendedTimeScale = 1f;
        float totalWeight = 0f;

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0) continue;

            var inputPlayable = (ScriptPlayable<HitSlowPlayableBehaviour>)playable.GetInput(i);
            var behaviour = inputPlayable.GetBehaviour();

            // クリップ内の進行度を計算
            double clipTime = inputPlayable.GetTime();
            double clipDuration = inputPlayable.GetDuration();
            float progress = (float)(clipTime / clipDuration);

            // タイムスケールを計算
            float timeScale = CalculateTimeScale(behaviour, progress);
            blendedTimeScale = Mathf.Lerp(blendedTimeScale, timeScale, weight);
            totalWeight += weight;
        }

        if (totalWeight <= 0)
        {
            blendedTimeScale = 1f;
        }

        if (!Mathf.Approximately(_currentTimeScale, blendedTimeScale))
        {
            _currentTimeScale = blendedTimeScale;
            ApplyTimeScaleToTracks(blendedTimeScale);
        }
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        // Graph停止時にspeed=1にリセット
        ApplyTimeScaleToTracks(1f);
    }

    private float CalculateTimeScale(HitSlowPlayableBehaviour behaviour, float progress)
    {
        float totalDuration = behaviour.SlowDuration;
        float holdDuration = behaviour.CenterHoldTime;
        float transitionDuration = (totalDuration - holdDuration) * 0.5f;

        // 正規化された時間
        float centerStart = transitionDuration / totalDuration;
        float centerEnd = (transitionDuration + holdDuration) / totalDuration;

        if (progress < centerStart)
        {
            // イン（1.0 → centerTimeScale）
            float t = progress / centerStart;
            float easedT = DOVirtual.EasedValue(0f, 1f, t, behaviour.EaseType);
            return Mathf.Lerp(1f, behaviour.CenterTimeScale, easedT);
        }
        else if (progress < centerEnd)
        {
            // ループ（centerTimeScale固定）
            return behaviour.CenterTimeScale;
        }
        else
        {
            // アウト（centerTimeScale → 1.0）
            float outProgress = (progress - centerEnd) / (1f - centerEnd);
            float easedT = DOVirtual.EasedValue(0f, 1f, outProgress, behaviour.EaseType);
            return Mathf.Lerp(behaviour.CenterTimeScale, 1f, easedT);
        }
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
