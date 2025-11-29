using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using DG.Tweening;

/// <summary>
/// ヒットスローのPlayableAsset実装
/// </summary>
[System.Serializable]
public class HitSlowPlayableAsset : PlayableAsset, ITimelineClipAsset
{
    [SerializeField]
    [Tooltip("ヒットスローの時間（秒）")]
    private float slowDuration = 0.5f;

    [SerializeField]
    [Tooltip("中心のウェイト (0.0-1.0)")]
    [Range(0f, 1f)]
    private float centerWeight = 0.5f;

    [SerializeField]
    [Tooltip("中心のタイムスケール (0.0-1.0)")]
    [Range(0f, 1f)]
    private float centerTimeScale = 0.1f;

    [SerializeField]
    [Tooltip("中心の静止時間（ループ）")]
    private float centerHoldTime = 0.1f;

    [SerializeField]
    [Tooltip("イージングタイプ")]
    private Ease ease = Ease.InOutQuad;

    /// <summary>
    /// ヒットスローの時間
    /// </summary>
    public float SlowDuration
    {
        get => slowDuration;
        set => slowDuration = value;
    }

    /// <summary>
    /// 中心のウェイト
    /// </summary>
    public float CenterWeight
    {
        get => centerWeight;
        set => centerWeight = Mathf.Clamp01(value);
    }

    /// <summary>
    /// 中心のタイムスケール
    /// </summary>
    public float CenterTimeScale
    {
        get => centerTimeScale;
        set => centerTimeScale = value;
    }

    /// <summary>
    /// 中心の静止時間
    /// </summary>
    public float CenterHoldTime
    {
        get => centerHoldTime;
        set => centerHoldTime = value;
    }

    /// <summary>
    /// イージングタイプ
    /// </summary>
    public Ease EaseType
    {
        get => ease;
        set => ease = value;
    }

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<HitSlowPlayableBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.SlowDuration = slowDuration;
        behaviour.CenterWeight = centerWeight;
        behaviour.CenterTimeScale = centerTimeScale;
        behaviour.CenterHoldTime = centerHoldTime;
        behaviour.EaseType = ease;
        return playable;
    }

    public override double duration => slowDuration;
}
