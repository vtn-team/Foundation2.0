using System.Diagnostics;
using UnityEngine;

/// <summary>
/// リリース時のデバッグ処理を再定義するクラス
/// RELEASEシンボルが定義されている場合、Debug.Log/LogWarningは何もしない
/// </summary>
public static class ReleaseOverride
{
    /// <summary>
    /// ログ出力（リリース時は無効）
    /// </summary>
    /// <param name="message">メッセージ</param>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message)
    {
        UnityEngine.Debug.Log(message);
    }

    /// <summary>
    /// ログ出力（コンテキスト付き、リリース時は無効）
    /// </summary>
    /// <param name="message">メッセージ</param>
    /// <param name="context">コンテキストオブジェクト</param>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message, Object context)
    {
        UnityEngine.Debug.Log(message, context);
    }

    /// <summary>
    /// 警告ログ出力（リリース時は無効）
    /// </summary>
    /// <param name="message">メッセージ</param>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object message)
    {
        UnityEngine.Debug.LogWarning(message);
    }

    /// <summary>
    /// 警告ログ出力（コンテキスト付き、リリース時は無効）
    /// </summary>
    /// <param name="message">メッセージ</param>
    /// <param name="context">コンテキストオブジェクト</param>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object message, Object context)
    {
        UnityEngine.Debug.LogWarning(message, context);
    }

    /// <summary>
    /// エラーログ出力（リリース時も有効）
    /// </summary>
    /// <param name="message">メッセージ</param>
    public static void LogError(object message)
    {
        UnityEngine.Debug.LogError(message);
    }

    /// <summary>
    /// エラーログ出力（コンテキスト付き、リリース時も有効）
    /// </summary>
    /// <param name="message">メッセージ</param>
    /// <param name="context">コンテキストオブジェクト</param>
    public static void LogError(object message, Object context)
    {
        UnityEngine.Debug.LogError(message, context);
    }
}
