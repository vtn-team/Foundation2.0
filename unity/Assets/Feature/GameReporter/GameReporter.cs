using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Unity.Profiling;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
/// <summary>
/// 実行中に様々な処理を記録するレポータークラス
/// </summary>
public class GameReporter : MonoBehaviour
{
    /// <summary>
    /// 記録フラグ
    /// </summary>
    [Flags]
    public enum RecordFlags
    {
        None = 0,
        Replay = 1 << 0,
        ErrorCheck = 1 << 1,
        CPUSpike = 1 << 2,
        Allocation = 1 << 3,
        Video = 1 << 4,
        All = Replay | ErrorCheck | CPUSpike | Allocation | Video
    }

    private static GameReporter _instance;

    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static GameReporter Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("GameReporter");
                _instance = go.AddComponent<GameReporter>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [SerializeField]
    private string reportOutputPath = "report";

    private bool _isRecording;
    private RecordFlags _activeFlags;
    private string _sessionId;

    // リプレイ記録用
    private InputEventTrace _inputTrace;
    private int _randomSeed;

    // エラーチェック用
    private HashSet<string> _recordedErrors = new HashSet<string>();
    private StringBuilder _errorLog = new StringBuilder();

    // CPUスパイク用
    private ProfilerRecorder _cpuRecorder;
    private ProfilerRecorder _drawCallRecorder;
    private float[] _frameTimeBuffer;
    private int _frameTimeIndex;
    private const int FrameBufferSize = 300; // 5秒分@60fps
    private float _averageFrameTime;
    private int _frameCount;

    // アロケーション用
    private ProfilerRecorder _gcAllocRecorder;
    private long[] _gcAllocBuffer;
    private int _gcAllocIndex;
    private const int GCAllocBufferSize = 1000;

    /// <summary>
    /// 記録中かどうか
    /// </summary>
    public bool IsRecording => _isRecording;

    /// <summary>
    /// アクティブな記録フラグ
    /// </summary>
    public RecordFlags ActiveFlags => _activeFlags;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _frameTimeBuffer = new float[FrameBufferSize];
        _gcAllocBuffer = new long[GCAllocBufferSize];

        Application.logMessageReceived += OnLogMessageReceived;
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLogMessageReceived;

        if (_isRecording)
        {
            StopRecording();
        }

        _cpuRecorder.Dispose();
        _drawCallRecorder.Dispose();
        _gcAllocRecorder.Dispose();
        _inputTrace?.Dispose();
    }

    private void Update()
    {
        if (!_isRecording) return;

        float frameTime = Time.unscaledDeltaTime;

        // CPU スパイク検出
        if ((_activeFlags & RecordFlags.CPUSpike) != 0)
        {
            UpdateCPUSpikeDetection(frameTime);
        }

        // アロケーション検出
        if ((_activeFlags & RecordFlags.Allocation) != 0)
        {
            UpdateAllocationDetection();
        }
    }

    /// <summary>
    /// 記録を開始する
    /// </summary>
    /// <param name="flags">記録フラグ</param>
    /// <param name="seed">ランダムシード（リプレイ用）</param>
    public void StartRecording(RecordFlags flags = RecordFlags.All, int seed = 0)
    {
        if (_isRecording) return;

        _isRecording = true;
        _activeFlags = flags;
        _sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _randomSeed = seed != 0 ? seed : UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        // レポートフォルダ作成
        string outputDir = Path.Combine(Application.dataPath, "..", reportOutputPath, _sessionId);
        Directory.CreateDirectory(outputDir);

        // リプレイ記録開始
        if ((flags & RecordFlags.Replay) != 0)
        {
            _inputTrace = new InputEventTrace();
            _inputTrace.Enable();
        }

        // エラーチェック初期化
        if ((flags & RecordFlags.ErrorCheck) != 0)
        {
            _recordedErrors.Clear();
            _errorLog.Clear();
        }

        // CPUスパイク監視開始
        if ((flags & RecordFlags.CPUSpike) != 0)
        {
            _cpuRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            _drawCallRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
            _frameTimeIndex = 0;
            _frameCount = 0;
            _averageFrameTime = 0;
        }

        // アロケーション監視開始
        if ((flags & RecordFlags.Allocation) != 0)
        {
            _gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            _gcAllocIndex = 0;
        }

        Debug.Log($"[GameReporter] Recording started. Session: {_sessionId}, Seed: {_randomSeed}");
    }

    /// <summary>
    /// 記録を停止してレポートを出力する
    /// </summary>
    public void StopRecording()
    {
        if (!_isRecording) return;

        string outputDir = Path.Combine(Application.dataPath, "..", reportOutputPath, _sessionId);

        // リプレイデータ出力
        if ((_activeFlags & RecordFlags.Replay) != 0)
        {
            SaveReplayData(outputDir);
        }

        // エラーログ出力
        if ((_activeFlags & RecordFlags.ErrorCheck) != 0)
        {
            SaveErrorLog(outputDir);
        }

        // CPU統計出力
        if ((_activeFlags & RecordFlags.CPUSpike) != 0)
        {
            SaveCPUStats(outputDir);
            _cpuRecorder.Dispose();
            _drawCallRecorder.Dispose();
        }

        // アロケーション統計出力
        if ((_activeFlags & RecordFlags.Allocation) != 0)
        {
            SaveAllocationStats(outputDir);
            _gcAllocRecorder.Dispose();
        }

        _isRecording = false;
        Debug.Log($"[GameReporter] Recording stopped. Output: {outputDir}");
    }

    /// <summary>
    /// ログメッセージ受信時のコールバック
    /// </summary>
    private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (!_isRecording) return;
        if ((_activeFlags & RecordFlags.ErrorCheck) == 0) return;

        if (type == LogType.Error || type == LogType.Exception)
        {
            string errorKey = condition.GetHashCode().ToString();

            if (!_recordedErrors.Contains(errorKey))
            {
                _recordedErrors.Add(errorKey);

                _errorLog.AppendLine($"[Frame {Time.frameCount}] {type}: {condition}");
                _errorLog.AppendLine(stackTrace);
                _errorLog.AppendLine();

                // スクリーンキャプチャ
                string outputDir = Path.Combine(Application.dataPath, "..", reportOutputPath, _sessionId);
                CaptureScreenshot(outputDir, $"error_{Time.frameCount}");
            }
        }
    }

    /// <summary>
    /// CPUスパイク検出更新
    /// </summary>
    private void UpdateCPUSpikeDetection(float frameTime)
    {
        _frameTimeBuffer[_frameTimeIndex] = frameTime;
        _frameTimeIndex = (_frameTimeIndex + 1) % FrameBufferSize;
        _frameCount++;

        // 平均更新
        if (_frameCount > FrameBufferSize)
        {
            float sum = 0;
            for (int i = 0; i < FrameBufferSize; i++)
            {
                sum += _frameTimeBuffer[i];
            }
            _averageFrameTime = sum / FrameBufferSize;

            // 60FPS = 16.67ms、これを下回る（時間がかかる）場合はスパイク
            if (frameTime > 0.0167f && frameTime > _averageFrameTime * 2f)
            {
                string outputDir = Path.Combine(Application.dataPath, "..", reportOutputPath, _sessionId);
                CaptureScreenshot(outputDir, $"spike_{Time.frameCount}");
                Debug.LogWarning($"[GameReporter] CPU Spike detected at frame {Time.frameCount}: {frameTime * 1000:F2}ms (avg: {_averageFrameTime * 1000:F2}ms)");
            }
        }
    }

    /// <summary>
    /// アロケーション検出更新
    /// </summary>
    private void UpdateAllocationDetection()
    {
        if (_gcAllocRecorder.Valid && _gcAllocIndex < GCAllocBufferSize)
        {
            long allocated = _gcAllocRecorder.LastValue;
            if (allocated > 0)
            {
                _gcAllocBuffer[_gcAllocIndex++] = allocated;
            }
        }
    }

    /// <summary>
    /// リプレイデータを保存
    /// </summary>
    private void SaveReplayData(string outputDir)
    {
        if (_inputTrace == null) return;

        string filePath = Path.Combine(outputDir, "replay.inputtrace");
        _inputTrace.WriteTo(filePath);

        // シード値も保存
        string seedPath = Path.Combine(outputDir, "replay_seed.txt");
        File.WriteAllText(seedPath, _randomSeed.ToString());

        _inputTrace.Dispose();
        _inputTrace = null;
    }

    /// <summary>
    /// エラーログを保存
    /// </summary>
    private void SaveErrorLog(string outputDir)
    {
        if (_errorLog.Length == 0) return;

        string filePath = Path.Combine(outputDir, "error_log.txt");
        File.WriteAllText(filePath, _errorLog.ToString());
    }

    /// <summary>
    /// CPU統計を保存
    /// </summary>
    private void SaveCPUStats(string outputDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Frame Time Statistics");
        sb.AppendLine($"Average Frame Time: {_averageFrameTime * 1000:F2}ms");
        sb.AppendLine($"Total Frames: {_frameCount}");

        string filePath = Path.Combine(outputDir, "cpu_stats.txt");
        File.WriteAllText(filePath, sb.ToString());
    }

    /// <summary>
    /// アロケーション統計を保存
    /// </summary>
    private void SaveAllocationStats(string outputDir)
    {
        if (_gcAllocIndex == 0) return;

        var sb = new StringBuilder();
        sb.AppendLine("GC Allocation Statistics");
        sb.AppendLine($"Total Allocations Recorded: {_gcAllocIndex}");

        long total = 0;
        for (int i = 0; i < _gcAllocIndex; i++)
        {
            total += _gcAllocBuffer[i];
        }
        sb.AppendLine($"Total Allocated: {total / 1024}KB");

        string filePath = Path.Combine(outputDir, "allocation_stats.txt");
        File.WriteAllText(filePath, sb.ToString());
    }

    /// <summary>
    /// スクリーンショットをキャプチャ
    /// </summary>
    private void CaptureScreenshot(string outputDir, string filename)
    {
        string filePath = Path.Combine(outputDir, $"{filename}.png");
        ScreenCapture.CaptureScreenshot(filePath);
    }
}
#endif
