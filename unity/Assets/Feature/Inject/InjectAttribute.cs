using System;

/// <summary>
/// 注入対象のフィールドを示すAttribute
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class InjectAttribute : Attribute
{
}
