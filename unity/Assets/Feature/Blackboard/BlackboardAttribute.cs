using System;

/// <summary>
/// グローバル参照対象のパラメータを示すAttribute
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class BlackboardAttribute : Attribute
{
    /// <summary>
    /// カテゴリ名
    /// </summary>
    public string Category { get; set; } = "";

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public BlackboardAttribute()
    {
    }

    /// <summary>
    /// コンストラクタ（カテゴリ指定）
    /// </summary>
    /// <param name="category">カテゴリ名</param>
    public BlackboardAttribute(string category)
    {
        Category = category;
    }
}
