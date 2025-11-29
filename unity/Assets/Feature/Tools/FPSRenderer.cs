using UnityEngine;
using TMPro;

/// <summary>
/// ゲームのFPSを計測してCanvas上に表示するコンポーネント
/// </summary>
public class FPSRenderer : MonoBehaviour
{
    [SerializeField]
    [Tooltip("FPS表示用のTextMeshPro")]
    private TextMeshProUGUI fpsText;

    [SerializeField]
    [Tooltip("更新間隔（秒）")]
    private float updateInterval = 0.5f;

    [SerializeField]
    [Tooltip("表示するかどうか")]
    private bool showFPS = true;

    private float _accumulatedTime;
    private int _frameCount;
    private float _currentFPS;
    private float _minFPS = float.MaxValue;
    private float _maxFPS;

    /// <summary>
    /// 表示フラグ
    /// </summary>
    public bool ShowFPS
    {
        get => showFPS;
        set
        {
            showFPS = value;
            UpdateVisibility();
        }
    }

    /// <summary>
    /// 現在のFPS
    /// </summary>
    public float CurrentFPS => _currentFPS;

    /// <summary>
    /// 最小FPS
    /// </summary>
    public float MinFPS => _minFPS;

    /// <summary>
    /// 最大FPS
    /// </summary>
    public float MaxFPS => _maxFPS;

    private void Awake()
    {
        if (fpsText == null)
        {
            fpsText = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    private void Start()
    {
        UpdateVisibility();
    }

    private void Update()
    {
        _accumulatedTime += Time.unscaledDeltaTime;
        _frameCount++;

        if (_accumulatedTime >= updateInterval)
        {
            _currentFPS = _frameCount / _accumulatedTime;

            if (_currentFPS < _minFPS && _frameCount > 10)
            {
                _minFPS = _currentFPS;
            }
            if (_currentFPS > _maxFPS)
            {
                _maxFPS = _currentFPS;
            }

            UpdateDisplay();

            _accumulatedTime = 0f;
            _frameCount = 0;
        }
    }

    /// <summary>
    /// 表示を更新
    /// </summary>
    private void UpdateDisplay()
    {
        if (!showFPS || fpsText == null) return;

        fpsText.text = $"FPS: {_currentFPS:F1}";
    }

    /// <summary>
    /// 表示状態を更新
    /// </summary>
    private void UpdateVisibility()
    {
        if (fpsText != null)
        {
            fpsText.gameObject.SetActive(showFPS);
        }
    }

    /// <summary>
    /// 統計をリセット
    /// </summary>
    public void ResetStats()
    {
        _minFPS = float.MaxValue;
        _maxFPS = 0f;
    }

    /// <summary>
    /// 詳細情報を取得
    /// </summary>
    /// <returns>FPS詳細文字列</returns>
    public string GetDetailedInfo()
    {
        return $"Current: {_currentFPS:F1} | Min: {_minFPS:F1} | Max: {_maxFPS:F1}";
    }
}
