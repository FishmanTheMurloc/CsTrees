using System.Linq;
using System.Text;
using CsTrees.SourceGenerator.Analysis;
using CsTrees.SourceGenerator.Emitters;
using Microsoft.CodeAnalysis;
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
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Find all properties with [BlackboardKey] attribute
        var propertyDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => BehaviourSymbolCollector.IsCandidateProperty(node),
                transform: static (ctx, _) => BehaviourSymbolCollector.GetPropertyInfo(ctx))
            .Where(static info => info is not null);

        // Step 2: Group by containing class and collect
        var groupedByClass = propertyDeclarations
            .Collect()
            .SelectMany(static (infos, _) =>
                BehaviourSymbolCollector.GroupByClass(infos.Where(p => p is not null).Select(p => p!).ToList()));

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
            var classSource = PortPartialEmitter.Generate(classInfo.ClassName, classInfo.Namespace, classInfo.Properties, classInfo.Constructors);
            spc.AddSource($"{classInfo.ClassName}.g.cs", SourceText.From(classSource, Encoding.UTF8));

            // Generate TreeBuilder extension methods (only when opted in via [GenerateTreeBuilderExtension])
            if (classInfo.HasGenerateBuilderExtension)
            {
                if (classInfo.Constructors.Count == 0)
                {
                    // CST006: opted in but no private constructor to extend
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.CST006, classInfo.Location, classInfo.ClassName));
                }
                else
                {
                    var extensionSource = BuilderExtensionEmitter.Generate(classInfo.ClassName, classInfo.Namespace, classInfo.Properties, classInfo.Constructors);
                    spc.AddSource($"{classInfo.ClassName}BuilderExtensions.g.cs", SourceText.From(extensionSource, Encoding.UTF8));
                }
            }
        });

        // === Pipeline 3: TreeBuilder subclass preset methods ===
        // Scans partial TreeBuilder subclasses implementing IBehaviourCatalog<TCatalog>,
        // collects public factory methods from TCatalog, and generates public builder methods
        // that delegate via the Catalog property (Leaf / LeafWithBlackboard).
        var builderSubclasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => TreeBuilderSubclassCollector.IsCandidateClass(node),
                transform: static (ctx, _) => TreeBuilderSubclassCollector.GetSubclassInfo(ctx))
            .Where(static info => info is not null);

        context.RegisterSourceOutput(builderSubclasses, (spc, info) =>
        {
            var source = TreeBuilderSubclassEmitter.Generate(info!);
            spc.AddSource($"{info!.ClassName}.g.cs", SourceText.From(source, Encoding.UTF8));
        });

        // === Diagnostics ===
        // CST004: non-partial TreeBuilder subclass implementing IBehaviourCatalog
        var nonPartialSubclasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax c && c.BaseList is not null,
                transform: static (ctx, _) => TreeBuilderSubclassCollector.GetNonPartialDiagnosticInfo(ctx))
            .Where(static info => info is not null);

        context.RegisterSourceOutput(nonPartialSubclasses, (spc, info) =>
        {
            spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.CST004, info!.Location, info.ClassName));
        });

        // CST005: [GenerateTreeBuilderExtension] on a class without [BlackboardKey] or not a Behaviour subclass
        var attrDiagnostics = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax c && c.AttributeLists.Count > 0,
                transform: static (ctx, _) => TreeBuilderSubclassCollector.GetAttrDiagnosticInfo(ctx))
            .Where(static info => info is not null);

        context.RegisterSourceOutput(attrDiagnostics, (spc, info) =>
        {
            spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.CST005, info!.Location, info.ClassName));
        });
    }
}
