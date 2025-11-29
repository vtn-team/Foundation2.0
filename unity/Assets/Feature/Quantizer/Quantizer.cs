using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// BGMに応じてゲーム内アクションをクォンタイズするクラス
/// </summary>
public class Quantizer
{
    /// <summary>
    /// クォンタイズイベント情報
    /// </summary>
    public class QuantizeEvent
    {
        public int BeatDivision;
        public float OffsetSeconds;
        public Action Callback;
        public float ScheduledTime;
        public string Description;
    }

    private static Quantizer _instance;

    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static Quantizer Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new Quantizer();
            }
            return _instance;
        }
    }

    [SerializeField]
    [Tooltip("音楽の想定BPM")]
    private float bpm = 120f;

    private double _startTime;
    private bool _isRunning;
    private AudioSource _bgmSource;
    private List<QuantizeEvent> _pendingEvents = new List<QuantizeEvent>();

    /// <summary>
    /// BPM
    /// </summary>
    public float BPM
    {
        get => bpm;
        set => bpm = value;
    }

    /// <summary>
    /// 実行中かどうか
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// 登録されているイベント数
    /// </summary>
    public int PendingEventCount => _pendingEvents.Count;

    /// <summary>
    /// 登録されているイベント一覧（エディタ確認用）
    /// </summary>
    public IReadOnlyList<QuantizeEvent> PendingEvents => _pendingEvents;

    /// <summary>
    /// 1拍の長さ（秒）
    /// </summary>
    public float BeatDuration => 60f / bpm;

    /// <summary>
    /// 1小節の長さ（秒）4/4拍子想定
    /// </summary>
    public float MeasureDuration => BeatDuration * 4f;

    /// <summary>
    /// 現在の経過時間（秒）
    /// </summary>
    public double CurrentTime
    {
        get
        {
            if (!_isRunning) return 0;

            // BGMソースがある場合はその時間を使用
            if (_bgmSource != null && _bgmSource.isPlaying)
            {
                return _bgmSource.time;
            }

            return AudioSettings.dspTime - _startTime;
        }
    }

    /// <summary>
    /// 現在の拍数
    /// </summary>
    public float CurrentBeat => (float)(CurrentTime / BeatDuration);

    /// <summary>
    /// BGM再生プレイヤーを引数に取り、クォンタイズのリズム処理を開始する
    /// </summary>
    /// <param name="bgmSource">BGM再生用AudioSource</param>
    public void PlayAndSync(AudioSource bgmSource)
    {
        _bgmSource = bgmSource;
        _startTime = AudioSettings.dspTime;
        _isRunning = true;
        _pendingEvents.Clear();

        if (bgmSource != null && !bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    /// <summary>
    /// クォンタイズを停止する
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _pendingEvents.Clear();
        _bgmSource = null;
    }

    /// <summary>
    /// クォンタイズする（次の指定拍でコールバックを実行）
    /// </summary>
    /// <param name="callback">実行するコールバック</param>
    /// <param name="beatDivision">調整拍数（デフォルト16分音符）</param>
    /// <param name="offsetSeconds">ジャストタイミングまでの時間オフセット（秒）</param>
    /// <param name="description">イベントの説明（エディタ確認用）</param>
    public void Quantize(Action callback, int beatDivision = 16, float offsetSeconds = 0f, string description = null)
    {
        if (!_isRunning || callback == null) return;

        float nextQuantizedTime = GetNextQuantizedTime(beatDivision) + offsetSeconds;

        var evt = new QuantizeEvent
        {
            BeatDivision = beatDivision,
            OffsetSeconds = offsetSeconds,
            Callback = callback,
            ScheduledTime = nextQuantizedTime,
            Description = description ?? "Quantized Event"
        };

        _pendingEvents.Add(evt);
        ExecuteAtTimeAsync(evt).Forget();
    }

    /// <summary>
    /// クォンタイズされたタイマー（指定の拍の時間になったらコールバックを実行）
    /// </summary>
    /// <param name="beatCount">待機する拍数</param>
    /// <param name="callback">実行するコールバック</param>
    /// <param name="offsetSeconds">オフセット（秒）</param>
    /// <param name="description">イベントの説明（エディタ確認用）</param>
    public void QuantizeAction(int beatCount, Action callback, float offsetSeconds = 0f, string description = null)
    {
        if (!_isRunning || callback == null) return;

        float targetTime = (float)CurrentTime + (beatCount * BeatDuration) + offsetSeconds;

        var evt = new QuantizeEvent
        {
            BeatDivision = 0,
            OffsetSeconds = offsetSeconds,
            Callback = callback,
            ScheduledTime = targetTime,
            Description = description ?? $"QuantizeAction ({beatCount} beats)"
        };

        _pendingEvents.Add(evt);
        ExecuteAtTimeAsync(evt).Forget();
    }

    /// <summary>
    /// クォンタイズされたタイマー（指定の拍まで待つ）
    /// </summary>
    /// <param name="beatCount">待機する拍数</param>
    /// <param name="offsetSeconds">オフセット（秒）</param>
    /// <returns>UniTask</returns>
    public async UniTask QuantizeTimer(int beatCount, float offsetSeconds = 0f)
    {
        if (!_isRunning) return;

        float waitTime = (beatCount * BeatDuration) + offsetSeconds;
        await UniTask.Delay(TimeSpan.FromSeconds(waitTime));
    }

    /// <summary>
    /// 次の一小節までの時間を返す
    /// </summary>
    /// <returns>次の小節までの時間（秒）</returns>
    public float NextMeasureTime()
    {
        if (!_isRunning) return 0f;

        double currentTime = CurrentTime;
        double currentMeasure = currentTime / MeasureDuration;
        double nextMeasureStart = (Math.Floor(currentMeasure) + 1) * MeasureDuration;

        return (float)(nextMeasureStart - currentTime);
    }

    /// <summary>
    /// 次のクォンタイズされた時間を取得
    /// </summary>
    /// <param name="beatDivision">拍の分割数（4=4分音符、8=8分音符、16=16分音符）</param>
    /// <returns>次のクォンタイズ時間（秒）</returns>
    private float GetNextQuantizedTime(int beatDivision)
    {
        double currentTime = CurrentTime;

        // 1拍を分割した単位の長さ
        float divisionDuration = BeatDuration * 4f / beatDivision;

        // 現在の分割単位位置
        double currentDivision = currentTime / divisionDuration;

        // 次の分割単位の開始時間
        double nextDivisionStart = (Math.Floor(currentDivision) + 1) * divisionDuration;

        return (float)nextDivisionStart;
    }

    /// <summary>
    /// 指定時間にコールバックを実行
    /// </summary>
    private async UniTaskVoid ExecuteAtTimeAsync(QuantizeEvent evt)
    {
        float waitTime = evt.ScheduledTime - (float)CurrentTime;

        if (waitTime > 0)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(waitTime));
        }

        if (_isRunning && evt.Callback != null)
        {
            evt.Callback.Invoke();
        }

        _pendingEvents.Remove(evt);
    }
}
