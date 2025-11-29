using UnityEngine;

/// <summary>
/// Quantizerクラスのテストコンポーネント
/// </summary>
public class QuantizeTest : MonoBehaviour
{
    [SerializeField]
    [Tooltip("BPM設定")]
    private float bpm = 120f;

    [SerializeField]
    [Tooltip("クォンタイズビート分割数")]
    private int beatDivision = 16;

    [SerializeField]
    [Tooltip("テスト用AudioSource")]
    private AudioSource audioSource;

    private int _clickCount;

    private void Awake()
    {
        // Quantizerの設定
        Quantizer.Instance.BPM = bpm;
    }

    private void Start()
    {
        // BGMがある場合は同期再生を開始
        if (audioSource != null)
        {
            Quantizer.Instance.PlayAndSync(audioSource);
        }
    }

    private void Update()
    {
        // クリックまたはキー入力でテスト
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            ClickEvent();
        }
    }

    /// <summary>
    /// クリックイベント。クォンタイズされてイベントが発火する
    /// </summary>
    [ContextMenu("Click Event")]
    public void ClickEvent()
    {
        _clickCount++;
        int count = _clickCount;

        float clickTime = (float)Quantizer.Instance.CurrentTime;
        Debug.Log($"[QuantizeTest] Click #{count} at {clickTime:F3}s (Beat: {Quantizer.Instance.CurrentBeat:F2})");

        // クォンタイズされたコールバックを登録
        Quantizer.Instance.Quantize(() =>
        {
            float quantizedTime = (float)Quantizer.Instance.CurrentTime;
            float delay = quantizedTime - clickTime;
            Debug.Log($"[QuantizeTest] Quantized Event #{count} fired at {quantizedTime:F3}s (delay: {delay * 1000:F1}ms)");
        }, beatDivision, 0f, $"Click #{count}");
    }

    /// <summary>
    /// 次の小節まで待つテスト
    /// </summary>
    [ContextMenu("Wait Next Measure")]
    public void WaitNextMeasure()
    {
        float waitTime = Quantizer.Instance.NextMeasureTime();
        Debug.Log($"[QuantizeTest] Next measure in {waitTime:F3}s");
    }

    /// <summary>
    /// Quantizerを停止
    /// </summary>
    [ContextMenu("Stop Quantizer")]
    public void StopQuantizer()
    {
        Quantizer.Instance.Stop();
        Debug.Log("[QuantizeTest] Quantizer stopped");
    }

    /// <summary>
    /// BPMを変更
    /// </summary>
    /// <param name="newBpm">新しいBPM</param>
    public void SetBPM(float newBpm)
    {
        bpm = newBpm;
        Quantizer.Instance.BPM = newBpm;
        Debug.Log($"[QuantizeTest] BPM changed to {newBpm}");
    }
}
