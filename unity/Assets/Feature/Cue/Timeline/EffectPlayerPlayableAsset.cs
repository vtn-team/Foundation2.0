using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// EffectPlayerのPlayableAsset実装
/// </summary>
[System.Serializable]
public class EffectPlayerPlayableAsset : PlayableAsset, ITimelineClipAsset
{
    [SerializeField]
    [Tooltip("再生するエフェクトのPrefabキー")]
    [PrefabDictionaryFilter(typeof(BasicEffect))]
    private string prefabKey;

    [SerializeField]
    [Tooltip("クォンタイズ用のビート")]
    private int quantizeBeat = 16;

    /// <summary>
    /// Prefabキー
    /// </summary>
    public string PrefabKey
    {
        get => prefabKey;
        set => prefabKey = value;
    }

    /// <summary>
    /// クォンタイズビート
    /// </summary>
    public int QuantizeBeat
    {
        get => quantizeBeat;
        set => quantizeBeat = value;
    }

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<EffectPlayerPlayableBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.PrefabKey = prefabKey;
        behaviour.QuantizeBeat = quantizeBeat;
        return playable;
    }
}
