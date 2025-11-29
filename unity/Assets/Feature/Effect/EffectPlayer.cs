using System;
using UnityEngine;

/// <summary>
/// エフェクト再生を管理するPure Class
/// </summary>
[Serializable]
public class EffectPlayer
{
    [SerializeField]
    [Tooltip("クォンタイズするフレーム（デフォルト0）")]
    private int quantizeFrame = 0;

    [SerializeField]
    [Tooltip("PrefabDictionaryのキー")]
    [PrefabDictionaryFilter(typeof(BasicEffect))]
    private string prefabKey;

    [SerializeField]
    [Tooltip("再生時に対象オブジェクトの子供として生成する")]
    private Transform attachTarget;

    [SerializeField]
    [Tooltip("再生位置")]
    private Vector3 playPos;

    private BasicEffect _currentEffect;

    /// <summary>
    /// プレファブキー
    /// </summary>
    public string PrefabKey
    {
        get => prefabKey;
        set => prefabKey = value;
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
    /// 再生位置
    /// </summary>
    public Vector3 PlayPos
    {
        get => playPos;
        set => playPos = value;
    }

    /// <summary>
    /// 再生中かどうか
    /// </summary>
    public bool IsPlaying => _currentEffect != null && _currentEffect.IsPlaying;

    /// <summary>
    /// 現在のエフェクト
    /// </summary>
    public BasicEffect CurrentEffect => _currentEffect;

    /// <summary>
    /// エフェクトを再生する
    /// </summary>
    public void Play()
    {
        Play(prefabKey);
    }

    /// <summary>
    /// 指定したキーでエフェクトを再生する
    /// </summary>
    /// <param name="key">PrefabDictionaryのキー</param>
    public void Play(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

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
        else
        {
            _currentEffect.SetPosition(playPos);
        }

        _currentEffect.Play();
    }

    /// <summary>
    /// アタッチ対象を設定する
    /// </summary>
    /// <param name="target">アタッチ対象のTransform</param>
    public void SetAttachTarget(Transform target)
    {
        attachTarget = target;
    }

    /// <summary>
    /// 再生を停止する
    /// </summary>
    public void Stop()
    {
        if (_currentEffect != null)
        {
            _currentEffect.Stop();
            _currentEffect = null;
        }
    }
}
