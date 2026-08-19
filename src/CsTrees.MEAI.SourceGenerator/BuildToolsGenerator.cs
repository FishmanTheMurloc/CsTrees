using System.Text;
using CsTrees.MEAI.SourceGenerator.Analysis;
using CsTrees.MEAI.SourceGenerator.Emitters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CsTrees.MEAI.SourceGenerator;

/// <summary>
/// Source Generator that scans classes inheriting CsTrees.MEAI.BuildToolsBase&lt;TBuilder&gt;,
/// collects factory methods from the builder's IBehaviourCatalog members,
/// and generates synchronous tool methods returning ToolResult.
/// </summary>
[Generator]
public sealed class BuildToolsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var buildToolsClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => BuildToolsCollector.IsCandidateClass(node),
                transform: static (ctx, _) => BuildToolsCollector.GetClassInfo(ctx))
            .Where(static info => info is not null);

        context.RegisterSourceOutput(buildToolsClasses, (spc, info) =>
        {
            var i = info!;

            // CSTM001: 非 partial 无法生成
            if (!i.IsPartial)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.CSTM001, i.Location, i.ClassName));
                return;
            }

            // CSTM003: 缺少 [Description] 的警告（仍生成方法）
            foreach (var (methodName, location) in i.MissingDescriptions)
            {
                spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.CSTM003, location, methodName));
            }

            // CSTM002: 同名工厂方法冲突，不生成（避免产生重复成员）
            if (i.DuplicateMethods.Count > 0)
            {
                foreach (var (methodName, location) in i.DuplicateMethods)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(Diagnostics.CSTM002, location, methodName));
                }
                return;
            }

            var source = BuildToolsEmitter.Generate(i);
            spc.AddSource($"{i.ClassName}.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }
}
