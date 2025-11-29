using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Injectアトリビュートが付与されたフィールドを検出し、コードを自動生成する
/// </summary>
public static class InjectCodeBuilder
{
    private const string GENERATED_PATH = "Assets/Feature/Inject/Generated/";
    private const string INJECT_PARAM_LIST_PARAMS_FILE = "InjectParamListParams.cs";

    /// <summary>
    /// ドメインリロード時に自動実行
    /// </summary>
    [InitializeOnLoadMethod]
    private static void OnDomainReload()
    {
        // 少し遅延させて実行（コンパイル完了を待つ）
        EditorApplication.delayCall += () =>
        {
            GenerateInjectionCode();
        };
    }

    /// <summary>
    /// メニューから手動実行
    /// </summary>
    [MenuItem("Tools/Inject/Generate Injection Code")]
    public static void GenerateInjectionCode()
    {
        var injectFields = CollectInjectFields();

        if (injectFields.Count == 0)
        {
            Debug.Log("[InjectCodeBuilder] Injectフィールドが見つかりませんでした");
            return;
        }

        // 出力ディレクトリを作成
        if (!Directory.Exists(GENERATED_PATH))
        {
            Directory.CreateDirectory(GENERATED_PATH);
        }

        // InjectParamListParams.csを生成
        GenerateInjectParamListParams(injectFields);

        // 各クラス用のpartial classを生成
        GenerateClassInjectParams(injectFields);

        AssetDatabase.Refresh();
        Debug.Log($"[InjectCodeBuilder] コード生成完了: {injectFields.Count}個のフィールド");
    }

    /// <summary>
    /// Injectアトリビュートが付与されたフィールドを収集
    /// </summary>
    private static Dictionary<Type, List<FieldInfo>> CollectInjectFields()
    {
        var result = new Dictionary<Type, List<FieldInfo>>();

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            // Unityやシステムのアセンブリはスキップ
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
                        if (field.GetCustomAttribute<InjectAttribute>() != null)
                        {
                            if (!result.ContainsKey(type))
                            {
                                result[type] = new List<FieldInfo>();
                            }
                            result[type].Add(field);
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
    /// InjectParamListParams.csを生成
    /// </summary>
    private static void GenerateInjectParamListParams(Dictionary<Type, List<FieldInfo>> injectFields)
    {
        var sb = new StringBuilder();
        var usings = new HashSet<string>();

        // 必要なusingを収集
        foreach (var kvp in injectFields)
        {
            foreach (var field in kvp.Value)
            {
                CollectUsings(field.FieldType, usings);
            }
        }

        // ヘッダー
        sb.AppendLine("// このファイルは自動生成されています。編集しないでください。");
        sb.AppendLine("using UnityEngine;");

        foreach (var ns in usings.OrderBy(u => u))
        {
            if (ns != "UnityEngine" && ns != "System")
            {
                sb.AppendLine($"using {ns};");
            }
        }

        sb.AppendLine();
        sb.AppendLine("public partial class InjectParamList");
        sb.AppendLine("{");

        // 各フィールドに対してプロパティとSerializeFieldを生成
        foreach (var kvp in injectFields)
        {
            var className = kvp.Key.Name;
            sb.AppendLine($"    // {className}");

            foreach (var field in kvp.Value)
            {
                var fieldName = field.Name.TrimStart('_');
                var propertyName = ToUpperCamelCase(fieldName);
                var privateFieldName = "_" + fieldName;
                var typeName = GetTypeName(field.FieldType);
                var defaultValue = GetDefaultValueString(field);

                sb.AppendLine($"    [SerializeField]");
                sb.AppendLine($"    private {typeName} {privateFieldName}{defaultValue};");
                sb.AppendLine($"    public {typeName} {propertyName} => {privateFieldName};");
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        // ファイルに書き込み
        var filePath = Path.Combine(GENERATED_PATH, INJECT_PARAM_LIST_PARAMS_FILE);
        var newContent = sb.ToString();

        if (ShouldWriteFile(filePath, newContent))
        {
            File.WriteAllText(filePath, newContent);
        }
    }

    /// <summary>
    /// 各クラス用のpartial classを生成
    /// </summary>
    private static void GenerateClassInjectParams(Dictionary<Type, List<FieldInfo>> injectFields)
    {
        foreach (var kvp in injectFields)
        {
            var type = kvp.Key;
            var fields = kvp.Value;

            var sb = new StringBuilder();
            var usings = new HashSet<string>();

            foreach (var field in fields)
            {
                CollectUsings(field.FieldType, usings);
            }

            // ヘッダー
            sb.AppendLine("// このファイルは自動生成されています。編集しないでください。");
            sb.AppendLine("using UnityEngine;");

            foreach (var ns in usings.OrderBy(u => u))
            {
                if (ns != "UnityEngine" && ns != "System")
                {
                    sb.AppendLine($"using {ns};");
                }
            }

            sb.AppendLine();
            sb.AppendLine($"public partial class {type.Name}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// 注入パラメータを適用");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    private void ApplyInjectParams()");
            sb.AppendLine("    {");
            sb.AppendLine("        if (!InjectSystem.IsParamInjectSettingsAvailable()) return;");
            sb.AppendLine("        var settings = InjectSystem.ParamInjectSettingsProperty;");
            sb.AppendLine("        if (settings?.SelectedParamList == null) return;");
            sb.AppendLine("        var paramList = settings.SelectedParamList;");
            sb.AppendLine();

            foreach (var field in fields)
            {
                var fieldName = field.Name;
                var propertyName = ToUpperCamelCase(field.Name.TrimStart('_'));
                sb.AppendLine($"        {fieldName} = paramList.{propertyName};");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            // ファイルに書き込み
            var fileName = $"{type.Name}InjectParams.cs";
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

        // 基本型はスキップ
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
    /// フィールドのデフォルト値を文字列で取得
    /// </summary>
    private static string GetDefaultValueString(FieldInfo field)
    {
        // デフォルト値の取得は複雑なため、基本的には空文字を返す
        // 実際のデフォルト値はインスタンス化が必要
        return "";
    }

    /// <summary>
    /// アッパーキャメルケースに変換
    /// </summary>
    private static string ToUpperCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = char.ToUpper(input[0]) + input.Substring(1);
        return result;
    }

    /// <summary>
    /// ファイルを書き込むべきか判定（内容が変更された場合のみ）
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
