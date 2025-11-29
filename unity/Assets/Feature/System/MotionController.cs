using UnityEngine;

/// <summary>
/// プレイヤーのモーション制御を行うコンポーネント
/// </summary>
public class MotionController : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Animator参照")]
    private Animator _animator;

    private int _currentStateHash;

    /// <summary>
    /// Animator参照
    /// </summary>
    public Animator Animator
    {
        get => _animator;
        set => _animator = value;
    }

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }
    }

    /// <summary>
    /// アニメーションが再生中かどうか
    /// </summary>
    /// <returns>再生中の場合true</returns>
    public bool IsPlayingAnimation()
    {
        if (_animator == null) return false;

        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.normalizedTime < 1f && !_animator.IsInTransition(0);
    }

    /// <summary>
    /// 特定のステートが再生中かどうか
    /// </summary>
    /// <param name="stateName">ステート名</param>
    /// <returns>再生中の場合true</returns>
    public bool IsPlayingState(string stateName)
    {
        if (_animator == null) return false;

        int stateHash = Animator.StringToHash(stateName);
        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.shortNameHash == stateHash;
    }

    /// <summary>
    /// アニメーションの切り替え
    /// </summary>
    /// <param name="stateName">遷移先のステート名</param>
    public void ChangeAnimation(string stateName)
    {
        if (_animator == null) return;

        int stateHash = Animator.StringToHash(stateName);
        _currentStateHash = stateHash;
        _animator.Play(stateHash);
    }

    /// <summary>
    /// アニメーションの切り替え（クロスフェード）
    /// </summary>
    /// <param name="stateName">遷移先のステート名</param>
    /// <param name="transitionDuration">遷移時間</param>
    public void CrossFadeAnimation(string stateName, float transitionDuration = 0.1f)
    {
        if (_animator == null) return;

        int stateHash = Animator.StringToHash(stateName);
        _currentStateHash = stateHash;
        _animator.CrossFade(stateHash, transitionDuration);
    }

    /// <summary>
    /// トリガーを設定してアニメーションを遷移
    /// </summary>
    /// <param name="triggerName">トリガー名</param>
    public void TriggerAnimation(string triggerName)
    {
        if (_animator == null) return;

        _animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// Bool パラメータを設定
    /// </summary>
    /// <param name="paramName">パラメータ名</param>
    /// <param name="value">値</param>
    public void SetBool(string paramName, bool value)
    {
        if (_animator == null) return;

        _animator.SetBool(paramName, value);
    }

    /// <summary>
    /// Float パラメータを設定
    /// </summary>
    /// <param name="paramName">パラメータ名</param>
    /// <param name="value">値</param>
    public void SetFloat(string paramName, float value)
    {
        if (_animator == null) return;

        _animator.SetFloat(paramName, value);
    }

    /// <summary>
    /// Integer パラメータを設定
    /// </summary>
    /// <param name="paramName">パラメータ名</param>
    /// <param name="value">値</param>
    public void SetInteger(string paramName, int value)
    {
        if (_animator == null) return;

        _animator.SetInteger(paramName, value);
    }

    /// <summary>
    /// 現在のアニメーション進行度を取得（0.0〜1.0）
    /// </summary>
    /// <returns>正規化された時間</returns>
    public float GetNormalizedTime()
    {
        if (_animator == null) return 0f;

        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.normalizedTime;
    }

    /// <summary>
    /// アニメーション速度を設定
    /// </summary>
    /// <param name="speed">速度（1.0が通常）</param>
    public void SetSpeed(float speed)
    {
        if (_animator == null) return;

        _animator.speed = speed;
    }
}
