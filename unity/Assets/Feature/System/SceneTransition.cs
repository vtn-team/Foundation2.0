using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using DG.Tweening;

/// <summary>
/// 画面遷移時に特殊効果をかけるコンポーネント
/// </summary>
public class SceneTransition : MonoBehaviour
{
    private const string PrefabAddress = "Prefab/SceneTransitCanvas";

    private static SceneTransition _instance;

    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static SceneTransition Instance
    {
        get
        {
            if (_instance == null)
            {
                CreateInstance();
            }
            return _instance;
        }
    }

    [SerializeField]
    private Canvas _canvas;

    [SerializeField]
    private Image _fadeImage;

    [SerializeField]
    private RawImage _captureImage;

    [SerializeField]
    [Tooltip("燃えるフェード用マテリアル")]
    private Material _burningMaterial;

    private RenderTexture _captureTexture;
    private TransitionType _lastTransitionType;
    private float _lastFadeDuration;
    private Ease _lastInEase;
    private Ease _lastOutEase;
    private Material _burningMaterialInstance;

    // シェーダプロパティID
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
    private static readonly int NoiseScaleId = Shader.PropertyToID("_NoiseScale");
    private static readonly int BurnColorId = Shader.PropertyToID("_BurnColor");
    private static readonly int EmberColorId = Shader.PropertyToID("_EmberColor");

    /// <summary>
    /// 遷移タイプ
    /// </summary>
    public enum TransitionType
    {
        None,
        Fade,
        ScreenCut,
        Burning
    }

    /// <summary>
    /// 前回の遷移タイプ
    /// </summary>
    public TransitionType LastTransitionType => _lastTransitionType;

    private static void CreateInstance()
    {
        // Addressablesからプレファブを読み込む
        var handle = Addressables.LoadAssetAsync<GameObject>(PrefabAddress);
        handle.Completed += (AsyncOperationHandle<GameObject> op) =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                var instance = Instantiate(op.Result);
                _instance = instance.GetComponent<SceneTransition>();
                DontDestroyOnLoad(instance);
            }
            else
            {
                // フォールバック: 動的生成
                CreateFallbackInstance();
            }
        };
    }

    private static void CreateFallbackInstance()
    {
        var go = new GameObject("SceneTransition");
        _instance = go.AddComponent<SceneTransition>();
        _instance.CreateFallbackUI();
        DontDestroyOnLoad(go);
    }

    private void CreateFallbackUI()
    {
        // Canvas作成
        var canvasGo = new GameObject("TransitionCanvas");
        canvasGo.transform.SetParent(transform);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10000;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // FadeImage作成
        var fadeGo = new GameObject("FadeImage");
        fadeGo.transform.SetParent(canvasGo.transform);
        _fadeImage = fadeGo.AddComponent<Image>();
        _fadeImage.color = Color.black;
        _fadeImage.raycastTarget = false;

        var fadeRect = _fadeImage.rectTransform;
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.sizeDelta = Vector2.zero;
        fadeRect.anchoredPosition = Vector2.zero;

        // CaptureImage作成
        var captureGo = new GameObject("CaptureImage");
        captureGo.transform.SetParent(canvasGo.transform);
        _captureImage = captureGo.AddComponent<RawImage>();
        _captureImage.raycastTarget = false;

        var captureRect = _captureImage.rectTransform;
        captureRect.anchorMin = Vector2.zero;
        captureRect.anchorMax = Vector2.one;
        captureRect.sizeDelta = Vector2.zero;
        captureRect.anchoredPosition = Vector2.zero;

        // 初期状態は非表示
        _fadeImage.gameObject.SetActive(false);
        _captureImage.gameObject.SetActive(false);

        // 燃えるフェード用マテリアル作成
        CreateBurningMaterial();
    }

    /// <summary>
    /// 燃えるフェード用マテリアルを作成
    /// </summary>
    private void CreateBurningMaterial()
    {
        if (_burningMaterial != null)
        {
            _burningMaterialInstance = new Material(_burningMaterial);
        }
        else
        {
            // シェーダからマテリアルを動的生成
            var shader = Shader.Find("UI/BurningFade");
            if (shader != null)
            {
                _burningMaterialInstance = new Material(shader);
                _burningMaterialInstance.SetColor(BurnColorId, new Color(1f, 0.5f, 0f, 1f));
                _burningMaterialInstance.SetColor(EmberColorId, new Color(1f, 0.2f, 0f, 1f));
                _burningMaterialInstance.SetFloat(EdgeWidthId, 0.1f);
                _burningMaterialInstance.SetFloat(NoiseScaleId, 8f);
            }
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // マテリアル作成
        if (_burningMaterialInstance == null)
        {
            CreateBurningMaterial();
        }
    }

    private void OnDestroy()
    {
        if (_captureTexture != null)
        {
            _captureTexture.Release();
            Destroy(_captureTexture);
        }

        if (_burningMaterialInstance != null)
        {
            Destroy(_burningMaterialInstance);
        }
    }

    /// <summary>
    /// 前回の遷移パターンと対になる処理を実行する
    /// </summary>
    public async UniTask SceneInitTransition()
    {
        switch (_lastTransitionType)
        {
            case TransitionType.Fade:
                await FadeOut(_lastFadeDuration, _lastOutEase);
                break;
            case TransitionType.ScreenCut:
                await ScreenCutOut();
                break;
            case TransitionType.Burning:
                await BurningFadeOut(_lastFadeDuration, _lastOutEase);
                break;
        }

        _lastTransitionType = TransitionType.None;
    }

    /// <summary>
    /// シンプルなフェードで画面を黒くする
    /// </summary>
    /// <param name="duration">フェード時間</param>
    /// <param name="inEase">イン時のイージング</param>
    /// <param name="outEase">アウト時のイージング</param>
    public async UniTask FadeIn(float duration = 0.5f, Ease inEase = Ease.Linear, Ease outEase = Ease.Linear)
    {
        _lastTransitionType = TransitionType.Fade;
        _lastFadeDuration = duration;
        _lastInEase = inEase;
        _lastOutEase = outEase;

        if (_fadeImage == null) return;

        _fadeImage.gameObject.SetActive(true);
        _fadeImage.color = new Color(0, 0, 0, 0);

        await _fadeImage.DOFade(1f, duration).SetEase(inEase).AsyncWaitForCompletion();
    }

    /// <summary>
    /// シンプルなフェードを開ける
    /// </summary>
    /// <param name="duration">フェード時間</param>
    /// <param name="ease">イージング</param>
    public async UniTask FadeOut(float duration = 0.5f, Ease ease = Ease.Linear)
    {
        if (_fadeImage == null) return;

        _fadeImage.color = Color.black;
        _fadeImage.gameObject.SetActive(true);

        await _fadeImage.DOFade(0f, duration).SetEase(ease).AsyncWaitForCompletion();

        _fadeImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// 画面のキャプチャをとり、それをImageにはりつける
    /// </summary>
    public async UniTask ScreenIn()
    {
        _lastTransitionType = TransitionType.ScreenCut;

        await CaptureScreen();

        if (_captureImage != null)
        {
            _captureImage.texture = _captureTexture;
            _captureImage.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 画面のキャプチャを真っ二つにして次のシーンにうつる
    /// </summary>
    public async UniTask ScreenCutOut()
    {
        if (_captureImage == null) return;

        // 簡易実装: フェードアウト
        await _captureImage.DOFade(0f, 0.3f).AsyncWaitForCompletion();

        _captureImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// 燃えるフェードイン
    /// </summary>
    /// <param name="duration">時間</param>
    /// <param name="inEase">イージング</param>
    /// <param name="outEase">アウト時イージング</param>
    public async UniTask BurningFadeIn(float duration = 1f, Ease inEase = Ease.Linear, Ease outEase = Ease.Linear)
    {
        _lastTransitionType = TransitionType.Burning;
        _lastFadeDuration = duration;
        _lastInEase = inEase;
        _lastOutEase = outEase;

        await CaptureScreen();

        if (_captureImage != null)
        {
            _captureImage.texture = _captureTexture;
            _captureImage.gameObject.SetActive(true);

            // 燃えるシェーダを適用
            if (_burningMaterialInstance != null)
            {
                _captureImage.material = _burningMaterialInstance;
                _burningMaterialInstance.SetFloat(ProgressId, 0f);

                // Progressを0→1にアニメーション
                await DOTween.To(
                    () => _burningMaterialInstance.GetFloat(ProgressId),
                    x => _burningMaterialInstance.SetFloat(ProgressId, x),
                    1f,
                    duration
                ).SetEase(inEase).AsyncWaitForCompletion();
            }
            else
            {
                // フォールバック: 通常のフェード
                await UniTask.Delay(TimeSpan.FromSeconds(duration));
            }
        }
    }

    /// <summary>
    /// 燃えるフェードアウト
    /// </summary>
    /// <param name="duration">時間</param>
    /// <param name="ease">イージング</param>
    public async UniTask BurningFadeOut(float duration = 1f, Ease ease = Ease.Linear)
    {
        if (_captureImage == null) return;

        // 燃えるシェーダを適用
        if (_burningMaterialInstance != null)
        {
            _captureImage.material = _burningMaterialInstance;
            _burningMaterialInstance.SetFloat(ProgressId, 1f);

            // Progressを1→0にアニメーション（逆再生）
            await DOTween.To(
                () => _burningMaterialInstance.GetFloat(ProgressId),
                x => _burningMaterialInstance.SetFloat(ProgressId, x),
                0f,
                duration
            ).SetEase(ease).AsyncWaitForCompletion();

            _captureImage.material = null;
        }
        else
        {
            // フォールバック: 通常のフェード
            await _captureImage.DOFade(0f, duration).SetEase(ease).AsyncWaitForCompletion();
        }

        _captureImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// 画面をキャプチャ
    /// </summary>
    private async UniTask CaptureScreen()
    {
        await UniTask.WaitForEndOfFrame();

        if (_captureTexture == null)
        {
            _captureTexture = new RenderTexture(Screen.width, Screen.height, 0);
        }
        else if (_captureTexture.width != Screen.width || _captureTexture.height != Screen.height)
        {
            _captureTexture.Release();
            _captureTexture = new RenderTexture(Screen.width, Screen.height, 0);
        }

        ScreenCapture.CaptureScreenshotIntoRenderTexture(_captureTexture);
    }
}
