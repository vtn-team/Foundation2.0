using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// ループエフェクト再生を管理するPure Class
/// </summary>
[Serializable]
public class LoopEffectPlayer
{
    [SerializeField]
    [Tooltip("開始エフェクトのPrefabDictionaryキー")]
    [PrefabDictionaryFilter(typeof(BasicEffect))]
    private string prefabKeyIn;

    [SerializeField]
    [Tooltip("ループエフェクトのPrefabDictionaryキー")]
    [PrefabDictionaryFilter(typeof(BasicEffect))]
    private string prefabKeyLoop;

    [SerializeField]
    [Tooltip("終了エフェクトのPrefabDictionaryキー")]
    [PrefabDictionaryFilter(typeof(BasicEffect))]
    private string prefabKeyOut;

    [SerializeField]
    [Tooltip("エフェクトをアタッチする対象")]
    private Transform attachTarget;

    private BasicEffect _currentEffect;
    private bool _isPlaying;

    /// <summary>
    /// 開始エフェクトのキー
    /// </summary>
    public string PrefabKeyIn
    {
        get => prefabKeyIn;
        set => prefabKeyIn = value;
    }

    /// <summary>
    /// ループエフェクトのキー
    /// </summary>
    public string PrefabKeyLoop
    {
        get => prefabKeyLoop;
        set => prefabKeyLoop = value;
    }

    /// <summary>
    /// 終了エフェクトのキー
    /// </summary>
    public string PrefabKeyOut
    {
        get => prefabKeyOut;
        set => prefabKeyOut = value;
    }

    /// <summary>
    /// アタッチ対象
    /// </summary>
    public Transform AttachTarget
    {
        get => attachTarget;
        set => attachTarget = value;
    }

    /// <summary>
    /// 再生中かどうか
    /// </summary>
    public bool IsPlaying => _isPlaying;

    /// <summary>
    /// アタッチ対象を設定する
    /// </summary>
    /// <param name="target">アタッチ対象のTransform</param>
    public void SetAttachTarget(Transform target)
    {
        attachTarget = target;
    }

    /// <summary>
    /// ループエフェクトを再生開始する
    /// </summary>
    public void Play()
    {
        if (_isPlaying) return;

        _isPlaying = true;

        // 開始エフェクトを再生
        if (!string.IsNullOrEmpty(prefabKeyIn))
        {
            PlayForce(prefabKeyIn);

            // ループエフェクトが設定されている場合、開始エフェクト終了後にループを開始
            if (!string.IsNullOrEmpty(prefabKeyLoop) && _currentEffect != null)
            {
                PlayLoopAfterDelay(_currentEffect.Duration).Forget();
            }
        }
        else if (!string.IsNullOrEmpty(prefabKeyLoop))
        {
            // 開始エフェクトがない場合は直接ループを再生
            PlayForce(prefabKeyLoop);
        }
    }

    /// <summary>
    /// 遅延後にループエフェクトを再生
    /// </summary>
    private async UniTaskVoid PlayLoopAfterDelay(float delay)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay));

        if (_isPlaying && !string.IsNullOrEmpty(prefabKeyLoop))
        {
            PlayForce(prefabKeyLoop);
        }
    }

    /// <summary>
    /// 指定したキーでエフェクトを強制再生
    /// </summary>
    /// <param name="key">PrefabDictionaryのキー</param>
    private void PlayForce(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        // 現在のエフェクトを停止
        if (_currentEffect != null)
        {
            _currentEffect.Stop();
        }

        var instance = PrefabStock.CreateInstance(key);
        if (instance == null) return;

        _currentEffect = instance.GetComponent<BasicEffect>();
        if (_currentEffect == null)
        {
            PrefabStock.ReleaseInstance(instance);
            return;
        }

        // アタッチ対象がある場合は子供に設定
        if (attachTarget != null)
        {
            _currentEffect.Attach(attachTarget);
        }

        _currentEffect.Play();
    }

    /// <summary>
    /// ループエフェクトを終了する（フェードアウト）
    /// </summary>
    public void PlayOut()
    {
        if (!_isPlaying) return;

        _isPlaying = false;

        // 終了エフェクトを再生
        if (!string.IsNullOrEmpty(prefabKeyOut))
        {
            PlayForce(prefabKeyOut);
        }

        _currentEffect = null;
    }

    /// <summary>
    /// 強制的にエフェクトを停止する
    /// </summary>
    public void ForceStop()
    {
        _isPlaying = false;

        if (_currentEffect != null)
        {
            _currentEffect.Stop();
            _currentEffect = null;
        }
    }
}
