using Cysharp.Threading.Tasks;

/// <summary>
/// 演出シーケンスの基底クラス
/// 派生クラスはツールにより自動生成される
/// </summary>
public abstract class CueSequence
{
    /// <summary>
    /// 再生中かどうか
    /// </summary>
    public bool IsPlaying { get; protected set; }

    /// <summary>
    /// シーケンスを再生する
    /// </summary>
    /// <returns>再生完了を待つUniTask</returns>
    public abstract UniTask PlayAsync();

    /// <summary>
    /// シーケンスを停止する
    /// </summary>
    public virtual void Stop()
    {
        IsPlaying = false;
    }
}
