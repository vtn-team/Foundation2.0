using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

/// <summary>
/// ゲーム中の演出シーケンスを管理するオーケストレータ
/// </summary>
public class CueOrchestrator
{
    private static CueOrchestrator _instance;

    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static CueOrchestrator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new CueOrchestrator();
            }
            return _instance;
        }
    }

    private List<CueSequence> _playingSequences = new List<CueSequence>();

    /// <summary>
    /// 再生中のシーケンス数
    /// </summary>
    public int PlayingCount => _playingSequences.Count;

    /// <summary>
    /// 再生中のシーケンス一覧
    /// </summary>
    public IReadOnlyList<CueSequence> PlayingSequences => _playingSequences;

    /// <summary>
    /// CueSequenceを再生する（コールバック版）
    /// </summary>
    /// <param name="sequence">再生するシーケンス</param>
    /// <param name="onComplete">完了時コールバック</param>
    public void Play(CueSequence sequence, Action onComplete = null)
    {
        if (sequence == null) return;

        PlayInternalAsync(sequence, onComplete).Forget();
    }

    /// <summary>
    /// CueSequenceを再生する（async版）
    /// </summary>
    /// <param name="sequence">再生するシーケンス</param>
    /// <returns>再生完了を待つUniTask</returns>
    public async UniTask PlayAsync(CueSequence sequence)
    {
        if (sequence == null) return;

        _playingSequences.Add(sequence);

        try
        {
            await sequence.PlayAsync();
        }
        finally
        {
            _playingSequences.Remove(sequence);
        }
    }

    /// <summary>
    /// 全てのシーケンスを停止する
    /// </summary>
    public void StopAll()
    {
        foreach (var sequence in _playingSequences)
        {
            sequence.Stop();
        }
        _playingSequences.Clear();
    }

    /// <summary>
    /// 内部再生処理（コールバック付き）
    /// </summary>
    private async UniTaskVoid PlayInternalAsync(CueSequence sequence, Action onComplete)
    {
        _playingSequences.Add(sequence);

        try
        {
            await sequence.PlayAsync();
        }
        finally
        {
            _playingSequences.Remove(sequence);
            onComplete?.Invoke();
        }
    }
}
