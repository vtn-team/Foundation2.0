/// <summary>
/// WorldTimeComposerから時間コールバックを受け取るインターフェース
/// </summary>
public interface ITimeScaleTarget
{
    /// <summary>
    /// タイムスケール更新時に呼び出される
    /// </summary>
    /// <param name="timeScale">現在のタイムスケール（0.0〜1.0）</param>
    void OnTimeScaleUpdate(float timeScale);
}

/// <summary>
/// ITimeScaleTargetの別名（ヒットストップ用）
/// </summary>
public interface IHitStopTarget : ITimeScaleTarget
{
}
