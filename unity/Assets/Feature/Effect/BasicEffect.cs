using UnityEngine;

/// <summary>
/// 基本的なエフェクトの再生を管理するコンポーネント
/// </summary>
public class BasicEffect : MonoBehaviour, IHitStopTarget, IObjectPool
{
    [SerializeField]
    [Tooltip("ヒットストップやヒットスローの影響を受けるかどうか")]
    private bool hitStopEnable = true;

    [SerializeField]
    [Tooltip("再生位置")]
    private Vector3 playPos;

    [SerializeField]
    [Tooltip("エフェクトのParticleSystem参照")]
    private ParticleSystem particleSystem;

    private string _prefabKey;
    private bool _isOneShot = true;
    private float _originalSpeed = 1f;

    /// <summary>
    /// ヒットストップ有効/無効
    /// </summary>
    public bool HitStopEnable
    {
        get => hitStopEnable;
        set => hitStopEnable = value;
    }

    /// <summary>
    /// 再生位置
    /// </summary>
    public Vector3 PlayPos
    {
        get => playPos;
        set => playPos = value;
    }

    /// <summary>
    /// ParticleSystem参照
    /// </summary>
    public ParticleSystem ParticleSystem => particleSystem;

    /// <summary>
    /// エフェクトの長さ（秒）
    /// </summary>
    public float Duration
    {
        get
        {
            if (particleSystem != null)
            {
                return particleSystem.main.duration;
            }
            return 0f;
        }
    }

    /// <summary>
    /// 再生中かどうか
    /// </summary>
    public bool IsPlaying => particleSystem != null && particleSystem.isPlaying;

    private void Awake()
    {
        if (particleSystem == null)
        {
            particleSystem = GetComponent<ParticleSystem>();
        }

        if (particleSystem != null)
        {
            _originalSpeed = particleSystem.main.simulationSpeed;
        }
    }

    /// <summary>
    /// エフェクトを再生する
    /// </summary>
    public void Play()
    {
        if (particleSystem == null) return;

        transform.position = playPos;
        particleSystem.Play();

        // ワンショットの場合は再生終了後に自動でプールに返却
        if (_isOneShot)
        {
            Invoke(nameof(ReturnToPool), Duration);
        }
    }

    /// <summary>
    /// 再生位置を設定する
    /// </summary>
    /// <param name="position">再生位置</param>
    public void SetPosition(Vector3 position)
    {
        playPos = position;
        transform.position = position;
    }

    /// <summary>
    /// 特定のノードの子供として配置する
    /// </summary>
    /// <param name="parent">親ノード</param>
    public void Attach(Transform parent)
    {
        if (parent != null)
        {
            transform.SetParent(parent);
            transform.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// エフェクトを停止する
    /// </summary>
    public void Stop()
    {
        if (particleSystem != null)
        {
            particleSystem.Stop();
        }
        CancelInvoke(nameof(ReturnToPool));
    }

    /// <summary>
    /// タイムスケール更新時のコールバック
    /// </summary>
    /// <param name="timeScale">タイムスケール</param>
    public void OnTimeScaleUpdate(float timeScale)
    {
        if (!hitStopEnable) return;
        if (particleSystem == null) return;

        var main = particleSystem.main;
        main.simulationSpeed = _originalSpeed * timeScale;
    }

    /// <summary>
    /// オブジェクトプールに返却
    /// </summary>
    private void ReturnToPool()
    {
        Stop();
        PrefabStock.ReleaseInstance(gameObject);
    }

    #region IObjectPool Implementation

    public void OnPoolInstantiate(string prefabKey)
    {
        _prefabKey = prefabKey;
    }

    public void OnPoolUse()
    {
        // 再利用時の初期化
        if (particleSystem != null)
        {
            particleSystem.Clear();
            var main = particleSystem.main;
            main.simulationSpeed = _originalSpeed;
        }
    }

    public void OnPoolRelease()
    {
        // プール返却時の処理
        transform.SetParent(null);
        CancelInvoke(nameof(ReturnToPool));
    }

    public void OnPoolDestroy()
    {
        // 破棄時の処理
    }

    #endregion
}
