using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// ゲーム中の時間(TimeScale)を管理するクラス
/// </summary>
public class WorldTimeComposer
{
    private static WorldTimeComposer _instance;

    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static WorldTimeComposer Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new WorldTimeComposer();
            }
            return _instance;
        }
    }

    /// <summary>
    /// ヒットスローの状態
    /// </summary>
    public enum HitSlowPhase
    {
        None,
        In,
        Loop,
        Out
    }

    private List<ITimeScaleTarget> _timeScaleTargets = new List<ITimeScaleTarget>();
    private float _hitStopTime;
    private bool _isHitSlow;

    // ヒットスロー用パラメータ
    private float _slowDuration;
    private float _centerWeight;
    private float _centerTimeScale;
    private float _centerHoldTime;
    private Ease _ease;
    private float _slowElapsedTime;

    /// <summary>
    /// ヒットストップ中かどうか
    /// </summary>
    public bool IsHitStop => _hitStopTime > 0 && !_isHitSlow;

    /// <summary>
    /// ヒットスロー中かどうか
    /// </summary>
    public bool IsHitSlow => _hitStopTime > 0 && _isHitSlow;

    /// <summary>
    /// 現在のタイムスケール
    /// </summary>
    public float CurrentTimeScale { get; private set; } = 1f;

    /// <summary>
    /// 現在のヒットスローフェーズ
    /// </summary>
    public HitSlowPhase CurrentPhase { get; private set; } = HitSlowPhase.None;

    /// <summary>
    /// 登録されているターゲット数
    /// </summary>
    public int TargetCount => _timeScaleTargets.Count;

    /// <summary>
    /// GameManagerから呼び出される更新処理
    /// </summary>
    /// <param name="deltaTime">経過時間</param>
    public void Update(float deltaTime)
    {
        if (_hitStopTime <= 0)
        {
            CurrentTimeScale = 1f;
            CurrentPhase = HitSlowPhase.None;
            NotifyTimeScale(1f);
            return;
        }

        float timeScale;

        if (!_isHitSlow)
        {
            // ヒットストップ
            timeScale = 0f;
            _hitStopTime -= deltaTime;
        }
        else
        {
            // ヒットスロー
            timeScale = CalculateHitSlowTimeScale(deltaTime);
            _slowElapsedTime += deltaTime;
            _hitStopTime -= deltaTime;
        }

        CurrentTimeScale = timeScale;
        NotifyTimeScale(timeScale);
    }

    /// <summary>
    /// ITimeScaleTargetを登録する
    /// </summary>
    /// <param name="target">登録するターゲット</param>
    public void Register(ITimeScaleTarget target)
    {
        if (target != null && !_timeScaleTargets.Contains(target))
        {
            _timeScaleTargets.Add(target);
        }
    }

    /// <summary>
    /// ITimeScaleTargetを登録解除する
    /// </summary>
    /// <param name="target">解除するターゲット</param>
    public void Unregister(ITimeScaleTarget target)
    {
        if (target != null)
        {
            _timeScaleTargets.Remove(target);
        }
    }

    /// <summary>
    /// ヒットストップを実行する
    /// </summary>
    /// <param name="duration">かける時間（秒）</param>
    /// <param name="quantize">クォンタイズするかどうか</param>
    public void HitStop(float duration, bool quantize = true)
    {
        _hitStopTime = duration;
        _isHitSlow = false;

        if (quantize)
        {
            // クォンタイズされたタイミングで実行
            Quantizer.Instance.Quantize(() =>
            {
                // 既に別のヒットストップが発生している場合は上書きしない
            });
        }
    }

    /// <summary>
    /// ヒットスローを実行する
    /// </summary>
    /// <param name="duration">かける時間（秒）</param>
    /// <param name="centerWeight">中心のウェイト (0.0-1.0)</param>
    /// <param name="centerTimeScale">中心のタイムスケール</param>
    /// <param name="centerHoldTime">中心の静止時間</param>
    /// <param name="ease">イージングフラグ</param>
    /// <param name="quantize">クォンタイズするかどうか</param>
    public void HitSlow(float duration, float centerWeight, float centerTimeScale, float centerHoldTime, Ease ease = Ease.InOutQuad, bool quantize = true)
    {
        _hitStopTime = duration;
        _isHitSlow = true;
        _slowDuration = duration;
        _centerWeight = Mathf.Clamp01(centerWeight);
        _centerTimeScale = centerTimeScale;
        _centerHoldTime = centerHoldTime;
        _ease = ease;
        _slowElapsedTime = 0f;

        if (quantize)
        {
            // クォンタイズされたタイミングで実行
            Quantizer.Instance.Quantize(() =>
            {
                // 既に別のヒットスローが発生している場合は上書きしない
            });
        }
    }

    /// <summary>
    /// ヒットストップ/スローを強制終了する
    /// </summary>
    public void ForceStop()
    {
        _hitStopTime = 0f;
        _isHitSlow = false;
        CurrentTimeScale = 1f;
        CurrentPhase = HitSlowPhase.None;
        NotifyTimeScale(1f);
    }

    /// <summary>
    /// ヒットスローのタイムスケールを計算
    /// </summary>
    private float CalculateHitSlowTimeScale(float deltaTime)
    {
        // フェーズの時間配分を計算
        float totalDuration = _slowDuration;
        float holdDuration = _centerHoldTime;
        float transitionDuration = (totalDuration - holdDuration) * 0.5f;

        // 中心の開始・終了時間
        float centerStart = transitionDuration;
        float centerEnd = centerStart + holdDuration;

        float progress = _slowElapsedTime / totalDuration;

        if (_slowElapsedTime < centerStart)
        {
            // イン（1.0 → centerTimeScale）
            CurrentPhase = HitSlowPhase.In;
            float t = _slowElapsedTime / transitionDuration;
            float easedT = DOVirtual.EasedValue(0f, 1f, t, _ease);
            return Mathf.Lerp(1f, _centerTimeScale, easedT);
        }
        else if (_slowElapsedTime < centerEnd)
        {
            // ループ（centerTimeScale固定）
            CurrentPhase = HitSlowPhase.Loop;
            return _centerTimeScale;
        }
        else
        {
            // アウト（centerTimeScale → 1.0）
            CurrentPhase = HitSlowPhase.Out;
            float outProgress = (_slowElapsedTime - centerEnd) / transitionDuration;
            float easedT = DOVirtual.EasedValue(0f, 1f, outProgress, _ease);
            return Mathf.Lerp(_centerTimeScale, 1f, easedT);
        }
    }

    /// <summary>
    /// 全ターゲットにタイムスケールを通知
    /// </summary>
    private void NotifyTimeScale(float timeScale)
    {
        for (int i = _timeScaleTargets.Count - 1; i >= 0; i--)
        {
            var target = _timeScaleTargets[i];
            if (target == null)
            {
                _timeScaleTargets.RemoveAt(i);
                continue;
            }

            target.OnTimeScaleUpdate(timeScale);
        }
    }
}
