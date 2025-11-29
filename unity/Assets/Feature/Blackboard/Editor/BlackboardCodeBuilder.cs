using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Blackboardアトリビュートが付与されたパラメータを検出し、コードを自動生成する
/// </summary>
public static class BlackboardCodeBuilder
{
    private const string GENERATED_PATH = "Assets/Feature/Blackboard/Generated/";
    private const string BLACKBOARD_PARAMS_FILE = "Blackboard.Generated.cs";

    /// <summary>
    /// ドメインリロード時に自動実行
    /// </summary>
    [InitializeOnLoadMethod]
    private static void OnDomainReload()
    {
        EditorApplication.delayCall += () =>
        {
            GenerateCode();
        };
    }

    /// <summary>
    /// メニューから手動実行
    /// </summary>
    [MenuItem("Tools/Blackboard/Generate Code")]
    public static void GenerateCode()
    {
        var blackboardFields = CollectBlackboardFields();

        if (blackboardFields.Count == 0)
        {
            Debug.Log("[BlackboardCodeBuilder] Blackboardフィールドが見つかりませんでした");
            return;
        }

        // 出力ディレクトリを作成
        if (!Directory.Exists(GENERATED_PATH))
        {
            Directory.CreateDirectory(GENERATED_PATH);
        }

        // Blackboard.Generated.csを生成
        GenerateBlackboardParams(blackboardFields);

        // 各クラス用のpartial classを生成
        GenerateClassBlackboardParams(blackboardFields);

        AssetDatabase.Refresh();
        Debug.Log($"[BlackboardCodeBuilder] コード生成完了: {blackboardFields.Count}個のフィールド");
    }

    /// <summary>
    /// Blackboardアトリビュートが付与されたフィールドを収集
    /// </summary>
    private static Dictionary<Type, List<(FieldInfo field, BlackboardAttribute attr)>> CollectBlackboardFields()
    {
        var result = new Dictionary<Type, List<(FieldInfo, BlackboardAttribute)>>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var name = assembly.GetName().Name;
            if (name.StartsWith("Unity") || name.StartsWith("System") || name.StartsWith("mscorlib"))
            {
                continue;
            }

            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var field in fields)
                    {
                        var attr = field.GetCustomAttribute<BlackboardAttribute>();
                        if (attr != null)
                        {
                            if (!result.ContainsKey(type))
                            {
                                result[type] = new List<(FieldInfo, BlackboardAttribute)>();
                            }
                            result[type].Add((field, attr));
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // 一部の型が読み込めない場合はスキップ
            }
        }

        return result;
    }

    /// <summary>
    /// Blackboard.Generated.csを生成
    /// </summary>
    private static void GenerateBlackboardParams(Dictionary<Type, List<(FieldInfo field, BlackboardAttribute attr)>> blackboardFields)
    {
        var sb = new StringBuilder();
        var usings = new HashSet<string>();
        var propertyNames = new HashSet<string>();

        // 必要なusingを収集
        foreach (var kvp in blackboardFields)
        {
            foreach (var (field, _) in kvp.Value)
            {
                CollectUsings(field.FieldType, usings);
            }
        }

        // ヘッダー
        sb.AppendLine("// このファイルは自動生成されています。編集しないでください。");
        sb.AppendLine("using System;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using R3;");

        foreach (var ns in usings.OrderBy(u => u))
        {
            if (ns != "UnityEngine" && ns != "System" && ns != "R3")
            {
                sb.AppendLine($"using {ns};");
            }
        }

        sb.AppendLine();
        sb.AppendLine("public partial class Blackboard");
        sb.AppendLine("{");

        // 各フィールドに対してプロパティとReactivePropertyを生成
        foreach (var kvp in blackboardFields)
        {
            var className = kvp.Key.Name;
            sb.AppendLine($"    // {className}");

            foreach (var (field, attr) in kvp.Value)
            {
                var fieldName = field.Name.TrimStart('_');
                var propertyName = ToUpperCamelCase(fieldName);

                // 名前が被った場合はクラス名を先頭に付ける
                if (propertyNames.Contains(propertyName))
                {
                    propertyName = className + propertyName;
                }
                propertyNames.Add(propertyName);

                var typeName = GetTypeName(field.FieldType);
                var category = string.IsNullOrEmpty(attr.Category) ? "Default" : attr.Category;
                var reactivePropertyName = $"_{propertyName}Property";

                // ReactiveProperty
                sb.AppendLine($"    private ReactiveProperty<{typeName}> {reactivePropertyName};");

                // 値取得用プロパティ
                sb.AppendLine($"    public {typeName} {propertyName}");
                sb.AppendLine("    {");
                sb.AppendLine($"        get => {reactivePropertyName}?.Value ?? default;");
                sb.AppendLine("    }");

                // 登録関数
                sb.AppendLine($"    public void Register{propertyName}(ReactiveProperty<{typeName}> property)");
                sb.AppendLine("    {");
                sb.AppendLine($"        {reactivePropertyName} = property;");
                sb.AppendLine($"        Register(\"{propertyName}\", property, \"{category}\");");
                sb.AppendLine("    }");

                // 購読関数
                sb.AppendLine($"    public IDisposable Subscribe{propertyName}(Action<{typeName}> onNext)");
                sb.AppendLine("    {");
                sb.AppendLine($"        if ({reactivePropertyName} == null)");
                sb.AppendLine("        {");
                sb.AppendLine($"            Debug.LogWarning(\"[Blackboard] {propertyName} is not registered yet.\");");
                sb.AppendLine("            return Disposable.Empty;");
                sb.AppendLine("        }");
                sb.AppendLine($"        return Subscribe(\"{propertyName}\", {reactivePropertyName}, onNext, \"{category}\");");
                sb.AppendLine("    }");

                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        // ファイルに書き込み
        var filePath = Path.Combine(GENERATED_PATH, BLACKBOARD_PARAMS_FILE);
        var newContent = sb.ToString();

        if (ShouldWriteFile(filePath, newContent))
        {
            File.WriteAllText(filePath, newContent);
        }
    }

    /// <summary>
    /// 各クラス用のpartial classを生成
    /// </summary>
    private static void GenerateClassBlackboardParams(Dictionary<Type, List<(FieldInfo field, BlackboardAttribute attr)>> blackboardFields)
    {
        var globalPropertyNames = new HashSet<string>();

        // まず全てのプロパティ名を収集（重複チェック用）
        foreach (var kvp in blackboardFields)
        {
            foreach (var (field, _) in kvp.Value)
            {
                var fieldName = field.Name.TrimStart('_');
                globalPropertyNames.Add(ToUpperCamelCase(fieldName));
            }
        }

        foreach (var kvp in blackboardFields)
        {
            var type = kvp.Key;
            var fields = kvp.Value;

            var sb = new StringBuilder();
            var usings = new HashSet<string>();

            foreach (var (field, _) in fields)
            {
                CollectUsings(field.FieldType, usings);
            }

            // ヘッダー
            sb.AppendLine("// このファイルは自動生成されています。編集しないでください。");
            sb.AppendLine("using System;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using R3;");

            foreach (var ns in usings.OrderBy(u => u))
            {
                if (ns != "UnityEngine" && ns != "System" && ns != "R3")
                {
                    sb.AppendLine($"using {ns};");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"public partial class {type.Name}");
            sb.AppendLine("{");

            // ReactivePropertyの宣言
            foreach (var (field, attr) in fields)
            {
                var fieldName = field.Name.TrimStart('_');
                var propertyName = ToUpperCamelCase(fieldName);
                var typeName = GetTypeName(field.FieldType);

                sb.AppendLine($"    private ReactiveProperty<{typeName}> _{fieldName}Property;");
            }

            sb.AppendLine();

            // Blackboardへの登録メソッド
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Blackboardにパラメータを登録");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    protected void RegisterToBlackboard()");
            sb.AppendLine("    {");

            foreach (var (field, attr) in fields)
            {
                var fieldName = field.Name.TrimStart('_');
                var propertyName = ToUpperCamelCase(fieldName);
                var className = type.Name;
                var typeName = GetTypeName(field.FieldType);

                // 名前重複時の処理
                var registeredName = propertyName;
                var count = globalPropertyNames.Count(n => n == propertyName);
                if (count > 1)
                {
                    registeredName = className + propertyName;
                }

                sb.AppendLine($"        _{fieldName}Property = new ReactiveProperty<{typeName}>({field.Name});");
                sb.AppendLine($"        Blackboard.Instance.Register{registeredName}(_{fieldName}Property);");
            }

            sb.AppendLine("    }");

            // 値更新メソッド
            sb.AppendLine();
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Blackboardの値を更新");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    protected void UpdateBlackboardValues()");
            sb.AppendLine("    {");

            foreach (var (field, _) in fields)
            {
                var fieldName = field.Name.TrimStart('_');
                sb.AppendLine($"        if (_{fieldName}Property != null) _{fieldName}Property.Value = {field.Name};");
            }

            sb.AppendLine("    }");

            sb.AppendLine("}");

            // ファイルに書き込み
            var fileName = $"{type.Name}BlackboardParams.cs";
            var filePath = Path.Combine(GENERATED_PATH, fileName);
            var newContent = sb.ToString();

            if (ShouldWriteFile(filePath, newContent))
            {
                File.WriteAllText(filePath, newContent);
            }
        }
    }

    /// <summary>
    /// 型名を取得（ジェネリック対応）
    /// </summary>
    private static string GetTypeName(Type type)
    {
        if (type.IsArray)
        {
            return GetTypeName(type.GetElementType()) + "[]";
        }

        if (type.IsGenericType)
        {
            var genericTypeName = type.GetGenericTypeDefinition().Name;
            genericTypeName = genericTypeName.Substring(0, genericTypeName.IndexOf('`'));

            var genericArgs = type.GetGenericArguments();
            var argNames = string.Join(", ", genericArgs.Select(GetTypeName));

            return $"{genericTypeName}<{argNames}>";
        }

        // 基本型の短縮名
        if (type == typeof(int)) return "int";
        if (type == typeof(float)) return "float";
        if (type == typeof(double)) return "double";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(string)) return "string";
        if (type == typeof(long)) return "long";
        if (type == typeof(short)) return "short";
        if (type == typeof(byte)) return "byte";

        return type.Name;
    }

    /// <summary>
    /// 必要なusingを収集
    /// </summary>
    private static void CollectUsings(Type type, HashSet<string> usings)
    {
        if (type.IsArray)
        {
            CollectUsings(type.GetElementType(), usings);
            return;
        }

        if (type.IsGenericType)
        {
            if (!string.IsNullOrEmpty(type.Namespace))
            {
                usings.Add(type.Namespace);
            }

            foreach (var arg in type.GetGenericArguments())
            {
                CollectUsings(arg, usings);
            }
            return;
        }

        if (type.IsPrimitive || type == typeof(string))
        {
            return;
        }

        if (!string.IsNullOrEmpty(type.Namespace) &&
            !type.Namespace.StartsWith("System") &&
            type.Namespace != "UnityEngine")
        {
            usings.Add(type.Namespace);
        }
    }

    /// <summary>
    /// アッパーキャメルケースに変換
    /// </summary>
    private static string ToUpperCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpper(input[0]) + input.Substring(1);
    }

    /// <summary>
    /// ファイルを書き込むべきか判定
    /// </summary>
    private static bool ShouldWriteFile(string filePath, string newContent)
    {
        if (!File.Exists(filePath))
        {
            return true;
        }

        var existingContent = File.ReadAllText(filePath);
        return existingContent != newContent;
    }
}
