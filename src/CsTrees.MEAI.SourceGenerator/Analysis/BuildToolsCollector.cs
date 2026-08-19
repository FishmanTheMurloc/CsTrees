using System.Collections.Generic;
using System.Linq;
using CsTrees.MEAI.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CsTrees.MEAI.SourceGenerator.Analysis;

/// <summary>
/// 扫描继承 CsTrees.MEAI.BuildToolsBase&lt;TBuilder&gt; 的类，
/// 从 TBuilder 的 IBehaviourCatalog 成员收集工厂方法作为工具调用方法的声明来源。
/// 生成签名与 CsTrees.SourceGenerator 为 TreeBuilder 生成的方法保持一致
/// （跳过 blackboard、children、child 参数），生成方法直接调用 Builder 上的同名方法。
/// </summary>
internal static class BuildToolsCollector
{
    internal static bool IsCandidateClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDecl
            && classDecl.BaseList is not null;
    }

    internal static BuildToolsClassInfo? GetClassInfo(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
        if (classSymbol is null) return null;

        // 必须继承 BuildToolsBase<TBuilder>
        var builderType = GetBuilderTypeArgument(classSymbol);
        if (builderType is null) return null;

        var methods = CollectCatalogMethods(builderType);
        var baseToolMethods = CollectBaseToolMethods(classSymbol);
        var ownToolMethods = CollectOwnToolMethods(classSymbol);

        var duplicates = new List<(string, Location)>();
        foreach (var group in methods.GroupBy(m => m.MethodName).Where(g => g.Count() > 1))
        {
            duplicates.Add((group.Key, classDecl.GetLocation()));
        }

        var missingDescriptions = methods
            .Where(m => string.IsNullOrEmpty(m.Description))
            .Select(m => (m.MethodName, m.Location))
            .ToList();

        return new BuildToolsClassInfo
        {
            ClassName = classSymbol.Name,
            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
            IsPartial = classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)),
            Methods = methods,
            BaseToolMethods = baseToolMethods,
            OwnToolMethods = ownToolMethods,
            DuplicateMethods = duplicates,
            MissingDescriptions = missingDescriptions,
            Location = classDecl.GetLocation()
        };
    }

    /// <summary>
    /// 沿继承链查找 CsTrees.MEAI.BuildToolsBase&lt;TBuilder&gt;，
    /// 返回其泛型参数 TBuilder 的类型符号；未继承时返回 null。
    /// </summary>
    internal static INamedTypeSymbol? GetBuilderTypeArgument(INamedTypeSymbol classSymbol)
        => FindBuildToolsBase(classSymbol)?.TypeArguments[0] as INamedTypeSymbol;

    /// <summary>
    /// 沿继承链查找 CsTrees.MEAI.BuildToolsBase&lt;TBuilder&gt; 的类型符号；未继承时返回 null。
    /// </summary>
    internal static INamedTypeSymbol? FindBuildToolsBase(INamedTypeSymbol classSymbol)
    {
        INamedTypeSymbol? current = classSymbol.BaseType;
        while (current is not null)
        {
            if (current.IsGenericType
                && current.Name == "BuildToolsBase"
                && current.ContainingNamespace?.ToDisplayString() == "CsTrees.MEAI"
                && current.TypeArguments.Length == 1)
            {
                return current;
            }
            current = current.BaseType;
        }
        return null;
    }

    /// <summary>
    /// 枚举 BuildToolsBase&lt;TBuilder&gt; 直接声明的 public 实例工具方法
    /// （返回 ToolResult 或 Task&lt;ToolResult&gt;），返回其方法名列表。
    /// 用于生成 Tools 委托数组成员的基类部分。
    /// </summary>
    internal static List<string> CollectBaseToolMethods(INamedTypeSymbol classSymbol)
    {
        var baseType = FindBuildToolsBase(classSymbol);
        if (baseType is null) return new();

        var names = new List<string>();
        foreach (var member in baseType.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;
            if (method.IsStatic) continue;
            if (method.DeclaredAccessibility != Accessibility.Public) continue;
            if (!IsToolResultLike(method.ReturnType)) continue;
            names.Add(method.Name);
        }
        return names;
    }

    /// <summary>
    /// 枚举宿主类自身直接声明的 public 实例工具方法（返回 ToolResult 或 Task&lt;ToolResult&gt;），
    /// 返回其方法名列表。不含继承自基类的方法，也不含 SG 生成的目录方法（后者不在当前编译中）。
    /// </summary>
    internal static List<string> CollectOwnToolMethods(INamedTypeSymbol classSymbol)
    {
        var names = new List<string>();
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;
            if (method.IsStatic) continue;
            if (method.DeclaredAccessibility != Accessibility.Public) continue;
            if (!SymbolEqualityComparer.Default.Equals(method.ContainingType, classSymbol)) continue;
            if (!IsToolResultLike(method.ReturnType)) continue;
            names.Add(method.Name);
        }
        return names;
    }

    /// <summary>判断类型是否为 ToolResult 或 Task&lt;ToolResult&gt;（CsTrees.MEAI.ToolResult）。</summary>
    internal static bool IsToolResultLike(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && IsToolResult(named)) return true;

        return type is INamedTypeSymbol task
            && task.Name == "Task"
            && task.ContainingNamespace?.ToDisplayString() == "System.Threading.Tasks"
            && task.TypeArguments.Length == 1
            && task.TypeArguments[0] is INamedTypeSymbol arg
            && IsToolResult(arg);
    }

    /// <summary>判断类型是否为 CsTrees.MEAI.ToolResult（按名字匹配，兼容引用程序集）。</summary>
    internal static bool IsToolResult(INamedTypeSymbol type)
        => type.Name == "ToolResult"
            && type.ContainingNamespace?.ToDisplayString() == "CsTrees.MEAI"
            && type.Arity == 0;

    /// <summary>
    /// 扫描 Builder 类型中实现 IBehaviourCatalog 的字段和属性，
    /// 对每个 Catalog 的 public 工厂方法（返回 Behaviour 子类）生成 ToolMethodInfo。
    /// 与 CsTrees.SourceGenerator 的收集逻辑保持一致，确保生成的方法能正确调用
    /// Builder 上 SG 生成的同名方法。
    /// 类型判断采用名字匹配而非符号比较：Builder 与 Catalog 可能来自引用程序集，
    /// GetTypeByMetadataName 解析出的符号与 AllInterfaces/继承链中的符号不一定相等。
    /// </summary>
    internal static List<ToolMethodInfo> CollectCatalogMethods(INamedTypeSymbol builderType)
    {
        var format = SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
            | SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral);

        var methods = new List<ToolMethodInfo>();
        var seenCatalogTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var member in builderType.GetMembers())
        {
            INamedTypeSymbol? catalogType = null;
            if (member is IFieldSymbol field && !field.IsImplicitlyDeclared
                && field.Type is INamedTypeSymbol fieldType)
            {
                catalogType = fieldType;
            }
            else if (member is IPropertySymbol prop
                     && prop.Type is INamedTypeSymbol propType)
            {
                catalogType = propType;
            }

            if (catalogType is null || !IsCatalogType(catalogType)) continue;
            if (!seenCatalogTypes.Add(catalogType)) continue;

            foreach (var m in CollectCatalogFactories(catalogType, format))
            {
                methods.Add(m);
            }
        }
        return methods;
    }

    /// <summary>
    /// 收集 Catalog 类型的 public 实例工厂方法（返回 Behaviour 子类）。
    /// 参数剔除 blackboard、children、child，提取方法与参数的 [Description]。
    /// </summary>
    internal static List<ToolMethodInfo> CollectCatalogFactories(
        INamedTypeSymbol catalogType, SymbolDisplayFormat format)
    {
        var methods = new List<ToolMethodInfo>();
        foreach (var member in catalogType.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;
            if (method.MethodKind != MethodKind.Ordinary) continue;
            if (method.IsStatic) continue;
            if (method.DeclaredAccessibility != Accessibility.Public) continue;
            if (method.ReturnType is not INamedTypeSymbol returnType) continue;
            if (!IsBehaviourSubclass(returnType)) continue;

            var nodeType = IsCompositeSubclass(returnType) ? ToolNodeType.Composite
                            : IsDecoratorSubclass(returnType) ? ToolNodeType.Decorator
                            : ToolNodeType.Leaf;

            var parameters = new List<ToolParamInfo>();
            foreach (var p in method.Parameters)
            {
                // 与 Builder 侧 SG 生成逻辑一致：blackboard 由 Builder 作用域注入，
                // children/child 由 PushComposite/PushDecorator 的栈机制处理
                if (IsBlackboardType(p.Type)) continue;
                if (nodeType == ToolNodeType.Composite && IsChildrenParameter(p.Type)) continue;
                if (nodeType == ToolNodeType.Decorator && IsChildParameter(p.Type)) continue;

                string? defaultValue = null;
                if (p.HasExplicitDefaultValue)
                    defaultValue = FormatDefaultValue(p.ExplicitDefaultValue, p.Type);

                parameters.Add(new ToolParamInfo
                {
                    Type = p.Type.ToDisplayString(format),
                    Name = p.Name,
                    DefaultValue = defaultValue,
                    Description = GetDescription(p)
                });
            }

            methods.Add(new ToolMethodInfo
            {
                MethodName = method.Name,
                Description = GetDescription(method),
                NodeType = nodeType,
                Parameters = parameters,
                Location = method.Locations.FirstOrDefault() ?? Location.None
            });
        }
        return methods;
    }

    /// <summary>
    /// 提取符号上的 [Description]（System.ComponentModel.DescriptionAttribute 及其子类）文本。
    /// </summary>
    internal static string? GetDescription(ISymbol symbol)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            var attrClass = attr.AttributeClass;
            if (attrClass is null) continue;

            // 沿继承链查找 DescriptionAttribute，支持自定义子类
            for (var t = attrClass; t is not null; t = t.BaseType)
            {
                if (t.Name == "DescriptionAttribute"
                    && t.ContainingNamespace?.ToDisplayString() == "System.ComponentModel")
                {
                    return attr.ConstructorArguments.Length > 0
                        && attr.ConstructorArguments[0].Value is string s
                        ? s
                        : null;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 判断类型是否实现 CsTrees.FluentBuilder.IBehaviourCatalog（按接口名匹配）。
    /// </summary>
    internal static bool IsCatalogType(INamedTypeSymbol typeSymbol)
    {
        foreach (var iface in typeSymbol.AllInterfaces)
        {
            if (iface.Name == "IBehaviourCatalog"
                && iface.ContainingNamespace?.ToDisplayString() == "CsTrees.FluentBuilder"
                && iface.Arity == 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 判断类型是否继承自指定基类（沿继承链按名字匹配，含基类本身）。
    /// </summary>
    internal static bool InheritsFrom(INamedTypeSymbol typeSymbol, string ns, string baseName)
    {
        var current = typeSymbol;
        while (current is not null)
        {
            if (current.Name == baseName
                && current.ContainingNamespace?.ToDisplayString() == ns)
                return true;
            current = current.BaseType;
        }
        return false;
    }

    /// <summary>判断类型是否为 CsTrees.Behaviour 子类（含本身）。</summary>
    internal static bool IsBehaviourSubclass(INamedTypeSymbol typeSymbol)
        => InheritsFrom(typeSymbol, "CsTrees", "Behaviour");

    /// <summary>判断类型是否为 CsTrees.Composite 子类（含本身）。</summary>
    internal static bool IsCompositeSubclass(INamedTypeSymbol typeSymbol)
        => InheritsFrom(typeSymbol, "CsTrees", "Composite");

    /// <summary>判断类型是否为 CsTrees.Decorator 子类（含本身）。</summary>
    internal static bool IsDecoratorSubclass(INamedTypeSymbol typeSymbol)
        => InheritsFrom(typeSymbol, "CsTrees", "Decorator");

    /// <summary>判断参数类型是否为 CsTrees.Blackboard.Blackboard。</summary>
    internal static bool IsBlackboardType(ITypeSymbol typeSymbol)
        => typeSymbol is INamedTypeSymbol named
            && named.Name == "Blackboard"
            && named.ContainingNamespace?.ToDisplayString() == "CsTrees.Blackboard"
            && named.Arity == 0;

    /// <summary>判断参数类型是否为 IEnumerable&lt;CsTrees.Behaviour&gt;（Composite 的 children 参数）。</summary>
    internal static bool IsChildrenParameter(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedType
            && namedType.ConstructedFrom.Name == "IEnumerable"
            && namedType.ConstructedFrom.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic"
            && namedType.TypeArguments.Length == 1
            && namedType.TypeArguments[0] is INamedTypeSymbol element)
        {
            return element.Name == "Behaviour"
                && element.ContainingNamespace?.ToDisplayString() == "CsTrees"
                && element.Arity == 0;
        }
        return false;
    }

    /// <summary>判断参数类型是否为 CsTrees.Behaviour（Decorator 的 child 参数）。</summary>
    internal static bool IsChildParameter(ITypeSymbol typeSymbol)
        => typeSymbol is INamedTypeSymbol named
            && named.Name == "Behaviour"
            && named.ContainingNamespace?.ToDisplayString() == "CsTrees"
            && named.Arity == 0;

    /// <summary>
    /// 将参数默认值格式化为 C# 字面量；无法表示时返回 null（视为无默认值）。
    /// </summary>
    internal static string? FormatDefaultValue(object? value, ITypeSymbol paramType)
    {
        if (value is null)
        {
            var isNullable = paramType.IsReferenceType
                || paramType.NullableAnnotation == NullableAnnotation.Annotated;
            return isNullable ? "null" : null;
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
            int => value.ToString(),
            uint => value.ToString() + "U",
            long l => l.ToString() + "L",
            ulong ul => ul.ToString() + "UL",
            float f => f.ToString("R") + "f",
            double d => d.ToString("R"),
            decimal m => m.ToString() + "m",
            _ => null
        };
    }

    internal static string EscapeString(string? s)
    {
        if (s is null) return string.Empty;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
