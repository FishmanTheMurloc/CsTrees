using System.Collections.Generic;
using System.Linq;
using CsTrees.SourceGenerator.Emitters;
using CsTrees.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CsTrees.SourceGenerator.Analysis;

internal static class BehaviourSymbolCollector
{
    internal static bool IsCandidateProperty(SyntaxNode node)
    {
        return node is PropertyDeclarationSyntax propertyDecl
            && propertyDecl.AttributeLists.Count > 0;
    }

    internal static PropertyInfo? GetPropertyInfo(GeneratorSyntaxContext context)
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
            var format = SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
            valueType = valueTypeArg.ToDisplayString(format);
            var valueTypeNoNullable = valueTypeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            valueTypeRef = $"typeof({valueTypeNoNullable})";
        }
        else
        {
            valueType = propType.ToDisplayString() ?? "object";
            valueTypeRef = "typeof(object)";
        }

        // Collect constructor parameter info from the containing class
        var constructorParamsList = CollectConstructorParams(propertySymbol.ContainingType);

        // 检查所在类是否标注 [GenerateTreeBuilderExtension]（决定是否生成 TreeBuilder 扩展方法）
        var hasBuilderExtension = propertySymbol.ContainingType.GetAttributes().Any(a =>
            a.AttributeClass is not null
            && a.AttributeClass.Name == "GenerateTreeBuilderExtensionAttribute"
            && a.AttributeClass.ContainingNamespace?.ToDisplayString() == "CsTrees.FluentBuilder");

        return new PropertyInfo
        {
            PropertyName = propertySymbol.Name,
            ValueType = valueType,
            ValueTypeRef = valueTypeRef,
            DefaultKey = defaultKey,
            Access = access,
            ContainingClassName = propertySymbol.ContainingType.Name,
            ContainingNamespace = propertySymbol.ContainingType.ContainingNamespace.ToDisplayString(),
            IsPartial = IsPartialClass(propertyDecl),
            IsBehaviourSubclass = IsBehaviourSubclass(propertySymbol.ContainingType, semanticModel),
            HasValidType = isBlackboardKeyAccessType,
            HasGenerateBuilderExtension = hasBuilderExtension,
            ConstructorParamsList = constructorParamsList,
            Location = propertyDecl.GetLocation()
        };
    }

    /// <summary>
    /// Collects all private constructors defined directly in the class (not inherited).
    /// Only private constructors are extended with a generated public overload that injects
    /// the Blackboard and calls SetupPorts; public constructors are left for the user to handle.
    /// Returns a list of (type, name, defaultValue) tuples for each constructor's parameters.
    /// defaultValue is null if the parameter has no default value.
    /// </summary>
    internal static List<List<(string Type, string Name, string?)>> CollectConstructorParams(INamedTypeSymbol classSymbol)
    {
        // FullyQualifiedFormat + nullable annotation support
        var format = SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
            | SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral);

        var result = new List<List<(string Type, string Name, string?)>>();

        foreach (var constructor in classSymbol.InstanceConstructors)
        {
            // Only include private constructors defined directly in this class
            // (public constructors are left to the user; only private ones get a generated overload)
            if (constructor.DeclaredAccessibility != Accessibility.Private
                || !constructor.ContainingType.Equals(classSymbol, SymbolEqualityComparer.Default))
                continue;

            var paramsList = new List<(string Type, string Name, string?)>();
            foreach (var param in constructor.Parameters)
            {
                var paramType = param.Type.ToDisplayString(format);
                string? defaultValue = null;
                if (param.HasExplicitDefaultValue)
                {
                    defaultValue = CodeGenHelpers.FormatDefaultValue(param.ExplicitDefaultValue, param.Type);
                }
                paramsList.Add((paramType, param.Name, defaultValue));
            }
            result.Add(paramsList);
        }

        return result;
    }

    internal static bool IsPartialClass(PropertyDeclarationSyntax propertyDecl)
    {
        var classDecl = propertyDecl.Parent as ClassDeclarationSyntax;
        return classDecl is not null
            && classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
    }

    internal static bool IsBehaviourSubclass(INamedTypeSymbol classSymbol, SemanticModel semanticModel)
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

    internal static IEnumerable<ClassInfo> GroupByClass(List<PropertyInfo> properties)
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
                HasGenerateBuilderExtension = first.HasGenerateBuilderExtension,
                Location = classDecl?.GetLocation() ?? Location.None
            };
        }
    }
}
