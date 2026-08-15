using System.Collections.Generic;
using System.Linq;
using CsTrees.SourceGenerator.Emitters;
using CsTrees.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CsTrees.SourceGenerator.Analysis;

/// <summary>
/// 扫描 partial TreeBuilder 子类中包含 ICatalog 实例的字段或属性，
/// 从每个 Catalog 收集 public 工厂方法（返回 Behaviour 子类），作为声明来源。
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

        // 扫描类中实现 ICatalog 的字段和属性，收集所有 Catalog 的工厂方法
        var methods = CollectCatalogMembers(classSymbol, semanticModel);
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
    /// 遍历 Builder 类成员，找到实现 ICatalog 的非静态字段和属性，
    /// 对每个 Catalog 类型收集其工厂方法。
    /// </summary>
    internal static List<DeclMethodInfo> CollectCatalogMembers(INamedTypeSymbol builderType, SemanticModel semanticModel)
    {
        var iBehaviourCatalogInterface = semanticModel.Compilation.GetTypeByMetadataName("CsTrees.FluentBuilder.IBehaviourCatalog");
        if (iBehaviourCatalogInterface is null) return new();

        var methods = new List<DeclMethodInfo>();
        foreach (var member in builderType.GetMembers())
        {
            if (member is IFieldSymbol field  && !field.IsImplicitlyDeclared
                && field.Type is INamedTypeSymbol fieldType
                && IsCatalogType(fieldType, iBehaviourCatalogInterface))
            {
                methods.AddRange(CollectCatalogFactories(fieldType, semanticModel, field.Name));
            }
            else if (member is IPropertySymbol prop
                     && prop.Type is INamedTypeSymbol propType
                     && IsCatalogType(propType, iBehaviourCatalogInterface))
            {
                methods.AddRange(CollectCatalogFactories(propType, semanticModel, prop.Name));
            }
        }
        return methods;
    }

    /// <summary>
    /// 扫描 Catalog 类型的 public 实例方法中返回 Behaviour 子类的（工厂方法）。
    /// 根据返回类型判断节点类型（Composite/Decorator/Leaf），
    /// Composite 的 IEnumerable&lt;Behaviour&gt; 参数标记为 IsChildren，
    /// Decorator 的 Behaviour 参数标记为 IsChild。
    /// 签名生成时跳过 blackboard、children、child；调用时按原位置传入。
    /// </summary>
    internal static List<DeclMethodInfo> CollectCatalogFactories(INamedTypeSymbol catalogType, SemanticModel semanticModel, string catalogMember)
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

            // 根据返回类型确定节点类型
            var nodeType = IsCompositeSubclass(returnType, semanticModel) ? NodeType.Composite
                            : IsDecoratorSubclass(returnType, semanticModel) ? NodeType.Decorator
                            : NodeType.Leaf;

            var parameters = new List<ParamInfo>();
            foreach (var p in method.Parameters)
            {
                var isBB = IsBlackboardType(p.Type, semanticModel);
                var isChildren = nodeType == NodeType.Composite && IsChildrenParameter(p.Type, semanticModel);
                var isChild = nodeType == NodeType.Decorator && IsChildParameter(p.Type, semanticModel);
                string? defaultValue = null;
                if (!isBB && !isChildren && !isChild && p.HasExplicitDefaultValue)
                    defaultValue = CodeGenHelpers.FormatDefaultValue(p.ExplicitDefaultValue, p.Type);
                parameters.Add(new ParamInfo
                {
                    Type = p.Type.ToDisplayString(format),
                    Name = p.Name,
                    DefaultValue = defaultValue,
                    IsBlackboard = isBB,
                    IsChildren = isChildren,
                    IsChild = isChild
                });
            }
            methods.Add(new DeclMethodInfo
            {
                MethodName = method.Name,
                CatalogMember = catalogMember,
                NodeType = nodeType,
                Parameters = parameters
            });
        }
        return methods;
    }

    /// <summary>
    /// 判断类型是否实现 ICatalog 接口。
    /// </summary>
    internal static bool IsCatalogType(INamedTypeSymbol typeSymbol, INamedTypeSymbol icatalogInterface)
    {
        foreach (var iface in typeSymbol.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, icatalogInterface))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 判断类型是否为 Composite 子类（含 Composite 本身）。
    /// </summary>
    internal static bool IsCompositeSubclass(INamedTypeSymbol typeSymbol, SemanticModel semanticModel)
    {
        var compositeType = semanticModel.Compilation.GetTypeByMetadataName("CsTrees.Composite");
        if (compositeType is null) return false;

        var current = typeSymbol;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, compositeType))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    /// <summary>
    /// 判断类型是否为 Decorator 子类（含 Decorator 本身）。
    /// </summary>
    internal static bool IsDecoratorSubclass(INamedTypeSymbol typeSymbol, SemanticModel semanticModel)
    {
        var decoratorType = semanticModel.Compilation.GetTypeByMetadataName("CsTrees.Decorator");
        if (decoratorType is null) return false;

        var current = typeSymbol;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, decoratorType))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    /// <summary>
    /// 判断参数是否为 IEnumerable&lt;Behaviour&gt; 类型（Composite 的 children 参数）。
    /// </summary>
    internal static bool IsChildrenParameter(ITypeSymbol typeSymbol, SemanticModel semanticModel)
    {
        var enumerableBehaviour = semanticModel.Compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
        if (enumerableBehaviour is null) return false;

        if (typeSymbol is INamedTypeSymbol namedType
            && SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, enumerableBehaviour)
            && namedType.TypeArguments.Length == 1)
        {
            var elementType = namedType.TypeArguments[0];
            var behaviourType = semanticModel.Compilation.GetTypeByMetadataName("CsTrees.Behaviour");
            if (behaviourType is null) return false;
            return SymbolEqualityComparer.Default.Equals(elementType, behaviourType);
        }
        return false;
    }

    /// <summary>
    /// 判断参数是否为 Behaviour 类型（Decorator 的 child 参数）。
    /// </summary>
    internal static bool IsChildParameter(ITypeSymbol typeSymbol, SemanticModel semanticModel)
    {
        var behaviourType = semanticModel.Compilation.GetTypeByMetadataName("CsTrees.Behaviour");
        if (behaviourType is null) return false;
        return SymbolEqualityComparer.Default.Equals(typeSymbol, behaviourType);
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
    /// CST004：识别包含 IBehaviourCatalog 实例但未声明 partial 的 TreeBuilder 子类。
    /// </summary>
    internal static DiagnosticInfo? GetNonPartialDiagnosticInfo(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
        if (classSymbol is null) return null;
        if (!IsTreeBuilderSubclass(classSymbol, semanticModel)) return null;

        // 包含 ICatalog 实例才需要 partial 来承接生成代码
        var hasCatalogs = CollectCatalogMembers(classSymbol, semanticModel).Count > 0;
        if (!hasCatalogs) return null;

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
