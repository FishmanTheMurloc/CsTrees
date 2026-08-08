using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CsTrees.SourceGenerator;

/// <summary>
/// Source Generator that detects [BlackboardKey] attributes on Behaviour properties
/// and generates SetupPorts, Create, and GetPortDeclarations methods.
/// </summary>
[Generator]
public sealed class BlackboardKeyGenerator : IIncrementalGenerator
{
    // Full qualified type names to avoid namespace conflicts
    private const string BlackboardType = "CsTrees.Blackboard.Blackboard";
    private const string PortDeclarationType = "CsTrees.Blackboard.PortDeclaration";
    private const string AccessType = "CsTrees.Blackboard.Access";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Find all properties with [BlackboardKey] attribute
        var propertyDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsCandidateProperty(node),
                transform: static (ctx, _) => GetPropertyInfo(ctx))
            .Where(static info => info is not null);

        // Step 2: Group by containing class and collect
        var groupedByClass = propertyDeclarations
            .Collect()
            .SelectMany(static (infos, _) =>
            {
                var validProps = infos.Where(p => p is not null).Select(p => p!).ToList();
                return GroupByClass(validProps);
            });

        // Step 3: Generate source for each class + report diagnostics
        context.RegisterSourceOutput(groupedByClass, (spc, classInfo) =>
        {
            // Report diagnostics for invalid properties
            foreach (var prop in classInfo.InvalidProperties)
            {
                if (!prop.HasValidType)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.CST003,
                        prop.Location,
                        prop.PropertyName,
                        prop.ValueType));
                }
            }

            if (!classInfo.IsPartial)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CST001,
                    classInfo.Location,
                    classInfo.ClassName));
                return;
            }

            if (!classInfo.IsBehaviourSubclass)
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CST002,
                    classInfo.Location,
                    classInfo.ClassName));
                return;
            }

            // Generate partial class source
            var classSource = GenerateSource(classInfo.ClassName, classInfo.Namespace, classInfo.Properties, classInfo.Constructors);
            spc.AddSource($"{classInfo.ClassName}.g.cs", SourceText.From(classSource, Encoding.UTF8));

            // Generate TreeBuilder extension methods
            var extensionSource = GenerateBuilderExtensions(classInfo.ClassName, classInfo.Namespace, classInfo.Properties, classInfo.Constructors);
            spc.AddSource($"{classInfo.ClassName}BuilderExtensions.g.cs", SourceText.From(extensionSource, Encoding.UTF8));
        });
    }

    private static bool IsCandidateProperty(SyntaxNode node)
    {
        return node is PropertyDeclarationSyntax propertyDecl
            && propertyDecl.AttributeLists.Count > 0;
    }

    private static PropertyInfo? GetPropertyInfo(GeneratorSyntaxContext context)
    {
        var propertyDecl = (PropertyDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // Check for [BlackboardKey] attribute
        var blackboardKeyAttr = propertyDecl.AttributeLists
            .SelectMany(al => al.Attributes)
            .FirstOrDefault(attr =>
            {
                var attrType = semanticModel.GetTypeInfo(attr).Type;
                return attrType is not null
                    && attrType.Name == "BlackboardKeyAttribute"
                    && attrType.ContainingNamespace?.ToDisplayString() == "CsTrees.Blackboard";
            });

        if (blackboardKeyAttr is null)
            return null;

        var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDecl) as IPropertySymbol;
        if (propertySymbol is null)
            return null;

        // Extract attribute arguments: Key and Access
        string? defaultKey = null;
        var access = "Write"; // default

        if (blackboardKeyAttr.ArgumentList is not null)
        {
            foreach (var arg in blackboardKeyAttr.ArgumentList.Arguments)
            {
                if (arg.NameEquals is not null)
                {
                    var name = arg.NameEquals.Name.Identifier.ValueText;
                    if (name == "Access")
                    {
                        var accessValue = semanticModel.GetConstantValue(arg.Expression);
                        if (accessValue.HasValue && accessValue.Value is int accessInt)
                            access = accessInt switch
                            {
                                0 => "Read",
                                1 => "Write",
                                2 => "ExclusiveWrite",
                                _ => "Write"
                            };
                        else if (arg.Expression is MemberAccessExpressionSyntax memberAccess)
                            access = memberAccess.Name.Identifier.ValueText;
                    }
                    else if (name == "Key")
                    {
                        var keyValue = semanticModel.GetConstantValue(arg.Expression);
                        if (keyValue.HasValue && keyValue.Value is string s)
                            defaultKey = s;
                    }
                }
                else
                {
                    // Positional argument: the key string
                    var keyValue = semanticModel.GetConstantValue(arg.Expression);
                    if (keyValue.HasValue && keyValue.Value is string s)
                        defaultKey = s;
                }
            }
        }

        // Fallback: use property name as key
        if (defaultKey is null)
            defaultKey = propertySymbol.Name;

        // Check property type is BehaviourKeyAccess<T>
        var propType = propertySymbol.Type;
        var namedType = propType as INamedTypeSymbol;
        var isBlackboardKeyAccessType = namedType is not null
            && namedType.Name == "BehaviourKeyAccess"
            && namedType.ContainingNamespace?.ToDisplayString() == "CsTrees.Blackboard"
            && namedType.TypeArguments.Length == 1;

        string valueType;
        string valueTypeRef;
        if (isBlackboardKeyAccessType)
        {
            var valueTypeArg = namedType!.TypeArguments[0];
            valueType = valueTypeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            valueTypeRef = $"typeof({valueType})";
        }
        else
        {
            valueType = propType.ToDisplayString() ?? "object";
            valueTypeRef = "typeof(object)";
        }

        // Collect constructor parameter info from the containing class
        var constructorParamsList = CollectConstructorParams(propertySymbol.ContainingType);

        return new PropertyInfo
        {
            PropertyName = propertySymbol.Name,
            ValueType = valueType,
            ValueTypeRef = valueTypeRef,
            DefaultKey = defaultKey,
            Access = access,
            ContainingClassName = propertySymbol.ContainingType.Name,
            ContainingNamespace = propertySymbol.ContainingNamespace.ToDisplayString(),
            IsPartial = IsPartialClass(propertyDecl),
            IsBehaviourSubclass = IsBehaviourSubclass(propertySymbol.ContainingType, semanticModel),
            HasValidType = isBlackboardKeyAccessType,
            ConstructorParamsList = constructorParamsList,
            Location = propertyDecl.GetLocation()
        };
    }

    /// <summary>
    /// Collects all public constructors defined in the class (not inherited).
    /// Returns a list of (type, name, defaultValue) tuples for each constructor's parameters.
    /// defaultValue is null if the parameter has no default value.
    /// </summary>
    private static List<List<(string Type, string Name, string? DefaultValue)>> CollectConstructorParams(INamedTypeSymbol classSymbol)
    {
        // FullyQualifiedFormat + nullable annotation support
        var format = SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
            | SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral);

        var result = new List<List<(string Type, string Name, string?)>>();

        foreach (var constructor in classSymbol.InstanceConstructors)
        {
            // Only include public constructors defined directly in this class
            if (constructor.DeclaredAccessibility != Accessibility.Public
                || !constructor.ContainingType.Equals(classSymbol, SymbolEqualityComparer.Default))
                continue;

            var paramsList = new List<(string Type, string Name, string?)>();
            foreach (var param in constructor.Parameters)
            {
                var paramType = param.Type.ToDisplayString(format);
                string? defaultValue = null;
                if (param.HasExplicitDefaultValue)
                {
                    defaultValue = FormatDefaultValue(param.ExplicitDefaultValue, param.Type);
                }
                paramsList.Add((paramType, param.Name, defaultValue));
            }
            result.Add(paramsList);
        }

        return result;
    }

    private static bool IsPartialClass(PropertyDeclarationSyntax propertyDecl)
    {
        var classDecl = propertyDecl.Parent as ClassDeclarationSyntax;
        return classDecl is not null
            && classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }

    private static bool IsBehaviourSubclass(INamedTypeSymbol classSymbol, SemanticModel semanticModel)
    {
        var behaviourType = semanticModel.Compilation.GetTypeByMetadataName("CsTrees.Behaviour");
        if (behaviourType is null)
            return false;

        var current = classSymbol;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, behaviourType))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static IEnumerable<ClassInfo> GroupByClass(List<PropertyInfo> properties)
    {
        var groups = properties.GroupBy(p => (p.ContainingClassName, p.ContainingNamespace));

        foreach (var group in groups)
        {
            var first = group.First();
            var classDecl = group.First().Location.SourceTree?.GetRoot()
                .FindNode(group.First().Location.SourceSpan)
                .Ancestors()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault();

            var validProperties = group.Where(p => p.HasValidType).ToList();
            var invalidProperties = group.Where(p => !p.HasValidType).ToList();

            if (validProperties.Count == 0 && invalidProperties.Count == 0)
                continue;

            // Deduplicate constructor params list (all properties from same class share the same)
            var constructors = first.ConstructorParamsList;

            yield return new ClassInfo
            {
                ClassName = group.Key.ContainingClassName,
                Namespace = group.Key.ContainingNamespace,
                Properties = validProperties,
                InvalidProperties = invalidProperties,
                Constructors = constructors,
                IsPartial = first.IsPartial,
                IsBehaviourSubclass = first.IsBehaviourSubclass,
                Location = classDecl?.GetLocation() ?? Location.None
            };
        }
    }

    private static string GenerateSource(string className, string ns, List<PropertyInfo> props, List<List<(string Type, string Name, string?)>> constructors)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(ns))
        {
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
        }

        sb.AppendLine($"    public partial class {className}");
        sb.AppendLine("    {");

        // === Constructors with blackboard (one per user constructor) ===
        GenerateConstructors(sb, className, props, constructors);

        sb.AppendLine();

        // === SetupPorts method ===
        GenerateSetupPorts(sb, props);

        sb.AppendLine();

        // === GetPortDeclarations method ===
        GenerateGetPortDeclarations(sb, props);

        sb.AppendLine("    }");

        if (!string.IsNullOrEmpty(ns))
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private static void AppendPortParams(StringBuilder sb, List<PropertyInfo> props, string prefix = "            ")
    {
        foreach (var prop in props)
        {
            var paramName = ToCamelCase(prop.PropertyName) + "Key";
            sb.AppendLine(",");
            sb.Append($"{prefix}string? {paramName} = null");
        }
    }

   private static void GenerateConstructors(StringBuilder sb, string className, List<PropertyInfo> props, List<List<(string Type, string Name, string?)>> constructors)
    {
        if (constructors.Count == 0)
        {
            // Fallback: assume user has (string name) constructor
            GenerateConstructor(sb, className, props, EmptyList);
            return;
        }

        foreach (var userParams in constructors)
        {
            GenerateConstructor(sb, className, props, userParams);
        }
    }

    private static readonly List<(string Type, string Name, string? DefaultValue)> EmptyList = new();

    private static void GenerateConstructor(StringBuilder sb, string className, List<PropertyInfo> props, List<(string Type, string Name, string? DefaultValue)> userParams)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// 创建实例并自动注册端口。");
        sb.AppendLine("        /// </summary>");

        sb.Append($"        public {className}(");

        // 分离必选与可选参数。Blackboard 是必选参数，必须出现在所有可选参数之前，
        // 否则会触发 CS1737（可选参数必须位于所有必选参数之后）。
        var requiredUserParams = userParams.Where(p => p.DefaultValue is null).ToList();
        var optionalUserParams = userParams.Where(p => p.DefaultValue is not null).ToList();

        // 必选用户参数（后面至少还有 Blackboard，总是需要逗号结尾）
        foreach (var (type, name, _) in requiredUserParams)
        {
            sb.AppendLine();
            sb.Append($"            {type} {name},");
        }

        // Blackboard（必选，插入在可选参数之前）
        sb.AppendLine();
        sb.Append($"            {BlackboardType} blackboard");

        // 可选用户参数
        foreach (var (type, name, defaultValue) in optionalUserParams)
        {
            sb.AppendLine(",");
            sb.Append($"            {type} {name}");
            if (defaultValue is not null)
                sb.Append($" = {defaultValue}");
        }

        // Optional port key parameters
        AppendPortParams(sb, props);

        sb.Append(") : this(");

        // Chain to user constructor
        for (int i = 0; i < userParams.Count; i++)
        {
            var (_, name, _) = userParams[i];
            sb.Append(name);
            if (i < userParams.Count - 1)
                sb.Append(", ");
        }

        sb.AppendLine(")");

        sb.AppendLine("        {");
        sb.Append("            SetupPorts(blackboard");

        foreach (var prop in props)
        {
            var paramName = ToCamelCase(prop.PropertyName) + "Key";
            sb.Append($", {paramName}: {paramName}");
        }

        sb.AppendLine(");");
        sb.AppendLine("        }");
    }

    private static void GenerateSetupPorts(StringBuilder sb, List<PropertyInfo> props)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// 在 Blackboard 上注册所有端口。");
        sb.AppendLine("        /// 必须在构造后、首次 Tick 前调用。");
        sb.AppendLine("        /// </summary>");
        sb.Append($"        public void SetupPorts({BlackboardType} blackboard");

        foreach (var prop in props)
        {
            var paramName = ToCamelCase(prop.PropertyName) + "Key";
            sb.AppendLine(",");
            sb.Append($"            string? {paramName} = null");
        }

        sb.AppendLine(")");

        sb.AppendLine("        {");

        foreach (var prop in props)
        {
            var paramName = ToCamelCase(prop.PropertyName) + "Key";
            var grantMethod = prop.Access switch
            {
                "Read" => "GrantRead",
                "Write" => "GrantWrite",
                "ExclusiveWrite" => "GrantExclusiveWrite",
                _ => "GrantWrite"
            };

            sb.AppendLine($"            {prop.PropertyName} = blackboard.{grantMethod}<{prop.ValueType}>(this, {paramName} ?? \"{EscapeString(prop.DefaultKey)}\");");
        }

        sb.AppendLine("        }");
    }

    private static void GenerateGetPortDeclarations(StringBuilder sb, List<PropertyInfo> props)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// 获取所有端口的声明信息。");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine($"        public static IReadOnlyDictionary<string, {PortDeclarationType}> GetPortDeclarations()");
        sb.AppendLine("        {");
        sb.AppendLine($"            return new Dictionary<string, {PortDeclarationType}>");
        sb.AppendLine("            {");

        foreach (var prop in props)
        {
            sb.AppendLine($"                [\"{EscapeString(prop.PropertyName)}\"] = new {PortDeclarationType}(\"{EscapeString(prop.DefaultKey)}\", {prop.ValueTypeRef}, {AccessType}.{prop.Access}),");
        }

        sb.AppendLine("            };");
        sb.AppendLine("        }");
    }

    /// <summary>
    /// Generate TreeBuilder extension methods for the given class.
    /// </summary>
    private static string GenerateBuilderExtensions(string className, string ns, List<PropertyInfo> props, List<List<(string Type, string Name, string?)>> constructors)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using CsTrees.Blackboard;");
        sb.AppendLine("using CsTrees.FluentBuilder;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(ns))
        {
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
        }

        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// TreeBuilder extension methods for {className}.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public static class {className}BuilderExtensions");
        sb.AppendLine("    {");

        GenerateBuilderExtensionMethods(sb, className, props, constructors);

        sb.AppendLine("    }");

        if (!string.IsNullOrEmpty(ns))
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private static void GenerateBuilderExtensionMethods(StringBuilder sb, string className, List<PropertyInfo> props, List<List<(string Type, string Name, string?)>> constructors)
    {
        if (constructors.Count == 0)
        {
            // Fallback: assume user has (string name) constructor
            GenerateBuilderExtensionMethod(sb, className, props, EmptyList);
            return;
        }

        foreach (var userParams in constructors)
        {
            GenerateBuilderExtensionMethod(sb, className, props, userParams);
        }
    }

    private static void GenerateBuilderExtensionMethod(StringBuilder sb, string className, List<PropertyInfo> props, List<(string Type, string Name, string?)> userParams)
    {
        sb.AppendLine("        /// <summary>");
        sb.AppendLine($"        /// Add a {className} node to the tree builder.");
        sb.AppendLine($"        /// The blackboard is automatically injected from the builder's current scope.");
        sb.AppendLine("        /// </summary>");

        sb.Append($"        public static TreeBuilder {className}(");
        sb.AppendLine();
        sb.AppendLine($"            this TreeBuilder builder,");
        sb.Append($"            string name");

        // User constructor parameters (skip 'name' parameter as it's always first)
        var paramsWithoutName = userParams.Skip(1).ToList();
        foreach (var (type, name, defaultValue) in paramsWithoutName)
        {
            sb.AppendLine(",");
            sb.Append($"            {type} {name}");
            if (defaultValue is not null)
                sb.Append($" = {defaultValue}");
        }

        // Optional port key parameters
        AppendPortParams(sb, props);

        sb.AppendLine(")");
        sb.AppendLine("        {");
        sb.AppendLine("            return builder.LeafWithBlackboard(bb => {");
        sb.AppendLine($"                if (bb is null)");
        sb.AppendLine($"                    throw new System.InvalidOperationException(\"Blackboard is required for {className}. Use .WithBlackboard() to set the scope.\");");
        sb.AppendLine();
        sb.Append($"                return new {className}(");

        // Pass name parameter (named: generated ctor reorders params, so positional
        // arguments would mismatch when the user ctor has optional params)
        sb.Append("name: name");

        // Pass other user params
        foreach (var (_, name, _) in paramsWithoutName)
        {
            sb.Append($", {name}: {name}");
        }

        // Pass blackboard
        sb.Append(", blackboard: bb");

        // Pass port key params
        foreach (var prop in props)
        {
            var paramName = ToCamelCase(prop.PropertyName) + "Key";
            sb.Append($", {paramName}: {paramName}");
        }

        sb.AppendLine(");");
        sb.AppendLine("            });");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private static string? FormatDefaultValue(object? value, ITypeSymbol paramType)
    {
        if (value is null)
        {
            // Check if the type is nullable (reference type or nullable value type)
            var isNullable = paramType.IsReferenceType
                || (paramType is ITypeSymbol ts && ts.NullableAnnotation == NullableAnnotation.Annotated);
            if (isNullable)
                return "null";
            return null;
        }

        return value switch
        {
            string s => $"\"{EscapeString(s)}\"",
            bool b => b ? "true" : "false",
            char c => $"'{c}'",
            byte => value.ToString(),
            sbyte => value.ToString(),
            short => value.ToString(),
            ushort => value.ToString(),
            int i => $"{i}",
            uint => value.ToString(),
            long l => $"{l}L",
            ulong ul => $"{ul}UL",
            float f => $"{f}f",
            double d => $"{d}",
            decimal m => $"{m}m",
            null => "null",
            _ => null
        };
    }

    private static string EscapeString(string? s)
    {
        if (s is null)
            return string.Empty;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private sealed class PropertyInfo
    {
        public string PropertyName { get; set; } = string.Empty;
        public string ValueType { get; set; } = string.Empty;
        public string ValueTypeRef { get; set; } = string.Empty;
        public string DefaultKey { get; set; } = string.Empty;
        public string Access { get; set; } = "Write";
        public string ContainingClassName { get; set; } = string.Empty;
        public string ContainingNamespace { get; set; } = string.Empty;
        public bool IsPartial { get; set; }
        public bool IsBehaviourSubclass { get; set; }
        public bool HasValidType { get; set; }
        public Location Location { get; set; } = Location.None;

        /// <summary>
        /// Constructor parameter info collected from the containing class.
        /// Each inner list represents one constructor's parameters.
        /// </summary>
        public List<List<(string Type, string Name, string? DefaultValue)>> ConstructorParamsList { get; set; } = new();
    }

    private sealed class ClassInfo
    {
        public string ClassName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public List<PropertyInfo> Properties { get; set; } = new();
        public List<PropertyInfo> InvalidProperties { get; set; } = new();
        public List<List<(string Type, string Name, string?)>> Constructors { get; set; } = new();
        public bool IsPartial { get; set; }
        public bool IsBehaviourSubclass { get; set; }
        public Location Location { get; set; } = Location.None;
    }
}
