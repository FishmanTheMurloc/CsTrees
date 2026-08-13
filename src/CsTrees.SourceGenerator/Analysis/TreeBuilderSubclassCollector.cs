using System.Collections.Generic;
using System.Linq;
using CsTrees.SourceGenerator.Emitters;
using CsTrees.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CsTrees.SourceGenerator.Analysis;

/// <summary>
/// 扫描实现 IBehaviourCatalog&lt;TCatalog&gt; 的 partial TreeBuilder 子类，
/// 从 TCatalog 收集 public 工厂方法（返回 Behaviour 子类），作为产物③的声明来源。
/// </summary>
internal static class TreeBuilderSubclassCollector
{
    internal static bool IsCandidateClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDecl
            && classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }

    internal static TreeBuilderSubclassInfo? GetSubclassInfo(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
        if (classSymbol is null) return null;

        // 必须是 TreeBuilder 的严格子类
        if (!IsTreeBuilderSubclass(classSymbol, semanticModel)) return null;

        // 必须实现 IBehaviourCatalog<TCatalog>，从中取出行为目录类型
        var catalogType = GetCatalogType(classSymbol, semanticModel.Compilation);
        if (catalogType is null) return null;

        // 扫描 TCatalog 的 public 工厂方法（返回 Behaviour 子类）
        var methods = CollectCatalogFactories(catalogType, semanticModel);
        if (methods.Count == 0) return null;

        return new TreeBuilderSubclassInfo
        {
            ClassName = classSymbol.Name,
            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
            Methods = methods,
            Location = classDecl.GetLocation()
        };
    }

    /// <summary>
    /// 从类的接口列表中找出 IBehaviourCatalog&lt;TCatalog&gt;，返回 TCatalog；未实现返回 null。
    /// </summary>
    internal static INamedTypeSymbol? GetCatalogType(INamedTypeSymbol classSymbol, Compilation compilation)
    {
        var catalogInterface = compilation.GetTypeByMetadataName("CsTrees.FluentBuilder.IBehaviourCatalog`1");
        if (catalogInterface is null) return null;

        foreach (var iface in classSymbol.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface.ConstructedFrom, catalogInterface)
                && iface.TypeArguments.Length == 1
                && iface.TypeArguments[0] is INamedTypeSymbol catalogType)
            {
                return catalogType;
            }
        }
        return null;
    }

    /// <summary>
    /// 扫描 TCatalog 的 public 实例方法中返回 Behaviour 子类的（工厂方法）。
    /// Parameters 保留所有参数（含 blackboard，用 IsBlackboard 标记），
    /// 以便生成签名时跳过 blackboard、生成调用时按原位置传 bb。
    /// </summary>
    internal static List<DeclMethodInfo> CollectCatalogFactories(INamedTypeSymbol catalogType, SemanticModel semanticModel)
    {
        var format = SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
            | SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral);

        var methods = new List<DeclMethodInfo>();
        foreach (var member in catalogType.GetMembers())
        {
            if (member is not IMethodSymbol method) continue;
            // 仅收集 public 实例方法（排除静态、构造函数、属性访问器等）
            if (method.MethodKind != MethodKind.Ordinary) continue;
            if (method.IsStatic) continue;
            if (method.DeclaredAccessibility != Accessibility.Public) continue;
            if (method.ReturnType is not INamedTypeSymbol returnType) continue;
            // 返回类型必须是 Behaviour 子类（含 Behaviour 本身）
            if (!BehaviourSymbolCollector.IsBehaviourSubclass(returnType, semanticModel)) continue;

            var parameters = new List<ParamInfo>();
            foreach (var p in method.Parameters)
            {
                var isBB = IsBlackboardType(p.Type, semanticModel);
                string? defaultValue = null;
                if (!isBB && p.HasExplicitDefaultValue)
                    defaultValue = CodeGenHelpers.FormatDefaultValue(p.ExplicitDefaultValue, p.Type);
                parameters.Add(new ParamInfo
                {
                    Type = p.Type.ToDisplayString(format),
                    Name = p.Name,
                    DefaultValue = defaultValue,
                    IsBlackboard = isBB
                });
            }
            methods.Add(new DeclMethodInfo
            {
                MethodName = method.Name,
                Parameters = parameters
            });
        }
        return methods;
    }

    /// <summary>
    /// 判断类型是否为 CsTrees.FluentBuilder.TreeBuilder<TBuilder> 的严格子类（排除 TreeBuilder 本身）。
    /// </summary>
    internal static bool IsTreeBuilderSubclass(INamedTypeSymbol classSymbol, SemanticModel semanticModel)
    {
        // 同时检查泛型定义和非泛型别名
        var treeBuilderGenericType = semanticModel.Compilation.GetTypeByMetadataName("CsTrees.FluentBuilder.TreeBuilder`1");
        var treeBuilderType = semanticModel.Compilation.GetTypeByMetadataName("CsTrees.FluentBuilder.TreeBuilder");

        if (treeBuilderGenericType is null && treeBuilderType is null) return false;
        if (treeBuilderType is not null && SymbolEqualityComparer.Default.Equals(classSymbol, treeBuilderType)) return false;

        var current = classSymbol.BaseType;
        while (current is not null)
        {
            if (treeBuilderType is not null && SymbolEqualityComparer.Default.Equals(current, treeBuilderType)) return true;
            if (treeBuilderGenericType is not null)
            {
                var constructedFrom = current.ConstructedFrom;
                if (SymbolEqualityComparer.Default.Equals(constructedFrom, treeBuilderGenericType))
                    return true;
            }
            current = current.BaseType;
        }
        return false;
    }

    /// <summary>
    /// 判断参数类型是否为 CsTrees.Blackboard.Blackboard（精确匹配，用于识别 blackboard 参数）。
    /// </summary>
    private static bool IsBlackboardType(ITypeSymbol typeSymbol, SemanticModel semanticModel)
    {
        var bbType = semanticModel.Compilation.GetTypeByMetadataName("CsTrees.Blackboard.Blackboard");
        if (bbType is null) return false;
        return SymbolEqualityComparer.Default.Equals(typeSymbol, bbType);
    }

    /// <summary>
    /// CST004：识别实现了 IBehaviourCatalog&lt;TCatalog&gt; 但未声明 partial 的 TreeBuilder 子类。
    /// </summary>
    internal static DiagnosticInfo? GetNonPartialDiagnosticInfo(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
        if (classSymbol is null) return null;
        if (!IsTreeBuilderSubclass(classSymbol, semanticModel)) return null;

        // 实现了 IBehaviourCatalog<TCatalog> 才需要 partial 来承接生成代码
        if (GetCatalogType(classSymbol, semanticModel.Compilation) is null) return null;

        var isPartial = classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
        if (isPartial) return null;

        return new DiagnosticInfo
        {
            ClassName = classSymbol.Name,
            Location = classDecl.GetLocation()
        };
    }

    /// <summary>
    /// CST005：识别标注 [GenerateTreeBuilderExtension] 但不符合生成条件的类
    /// （非 Behaviour 子类或无 [BlackboardKey] 属性）。
    /// </summary>
    internal static DiagnosticInfo? GetAttrDiagnosticInfo(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
        if (classSymbol is null) return null;

        // 仅处理标注了 [GenerateTreeBuilderExtension] 的类
        bool hasAttr = classSymbol.GetAttributes().Any(a =>
            a.AttributeClass is not null
            && a.AttributeClass.Name == "GenerateTreeBuilderExtensionAttribute"
            && a.AttributeClass.ContainingNamespace?.ToDisplayString() == "CsTrees.FluentBuilder");
        if (!hasAttr) return null;

        bool isBehaviourSubclass = BehaviourSymbolCollector.IsBehaviourSubclass(classSymbol, semanticModel);
        bool hasBlackboardKey = classSymbol.GetMembers().OfType<IPropertySymbol>().Any(p =>
            p.GetAttributes().Any(a =>
                a.AttributeClass is not null
                && a.AttributeClass.Name == "BlackboardKeyAttribute"
                && a.AttributeClass.ContainingNamespace?.ToDisplayString() == "CsTrees.Blackboard"));

        // 符合条件（Behaviour 子类 + 有 [BlackboardKey]）则不诊断
        if (isBehaviourSubclass && hasBlackboardKey) return null;

        return new DiagnosticInfo
        {
            ClassName = classSymbol.Name,
            Location = classDecl.GetLocation()
        };
    }
}
