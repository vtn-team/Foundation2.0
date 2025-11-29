using UnityEngine;

/// <summary>
/// EffectPlayerをインスペクタ上から操作するためのコンポーネント
/// </summary>
public class EffectPlayerComponent : MonoBehaviour
{
    [SerializeField]
    [Tooltip("PrefabDictionaryのキー")]
    [PrefabDictionaryFilter(typeof(BasicEffect))]
    private string prefabKey;

    [SerializeField]
    [Tooltip("再生時に対象オブジェクトの子供として生成する")]
    private Transform attachTarget;

    private EffectPlayer _effectPlayer;

    /// <summary>
    /// 内部のEffectPlayer
    /// </summary>
    public EffectPlayer EffectPlayer
    {
        get
        {
            if (_effectPlayer == null)
            {
                _effectPlayer = new EffectPlayer();
            }
            return _effectPlayer;
        }
    }

    /// <summary>
    /// プレファブキー
    /// </summary>
    public string PrefabKey
    {
        get => prefabKey;
        set
        {
            prefabKey = value;
            EffectPlayer.PrefabKey = value;
        }
    }

    /// <summary>
    /// アタッチ対象
    /// </summary>
    public Transform AttachTarget
    {
        get => attachTarget;
        set
        {
            attachTarget = value;
            EffectPlayer.AttachTarget = value;
        }
    }

    /// <summary>
    /// 再生中かどうか
    /// </summary>
    public bool IsPlaying => EffectPlayer.IsPlaying;

    private void Awake()
    {
        // EffectPlayerを初期化
        EffectPlayer.PrefabKey = prefabKey;
        EffectPlayer.AttachTarget = attachTarget;
    }

    /// <summary>
    /// エフェクトを再生する
    /// </summary>
    public void Play()
    {
        EffectPlayer.Play();
    }

    /// <summary>
    /// 指定したキーでエフェクトを再生する
    /// </summary>
    /// <param name="key">PrefabDictionaryのキー</param>
    public void Play(string key)
    {
        EffectPlayer.Play(key);
    }

    /// <summary>
    /// 再生を停止する
    /// </summary>
    public void Stop()
    {
        EffectPlayer.Stop();
    }

    /// <summary>
    /// アタッチ対象を設定する
    /// </summary>
    /// <param name="target">アタッチ対象のTransform</param>
    public void SetAttachTarget(Transform target)
    {
        attachTarget = target;
        EffectPlayer.SetAttachTarget(target);
    }
}
