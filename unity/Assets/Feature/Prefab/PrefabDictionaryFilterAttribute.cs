using System;
using UnityEngine;

/// <summary>
/// 特定のコンポーネントを持つPrefabのみをフィルタリングして表示するPropertyAttribute
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class PrefabDictionaryFilterAttribute : PropertyAttribute
{
    /// <summary>
    /// フィルタリングするコンポーネントの型
    /// </summary>
    public Type ComponentType { get; private set; }

    /// <summary>
    /// キーのプレフィックスフィルタ
    /// </summary>
    public string KeyPrefix { get; set; } = "";

    /// <summary>
    /// 空の選択肢を含めるか
    /// </summary>
    public bool IncludeEmpty { get; set; } = true;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="componentType">フィルタリングするコンポーネントの型</param>
    public PrefabDictionaryFilterAttribute(Type componentType)
    {
        ComponentType = componentType;
    }
}
