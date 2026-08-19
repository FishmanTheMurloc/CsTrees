using Microsoft.CodeAnalysis;

namespace CsTrees.MEAI.SourceGenerator;

internal static class Diagnostics
{
    public static DiagnosticDescriptor CSTM001 { get; } = new(
        id: "CSTM001",
        title: "BuildToolsBase<TBuilder> 派生类必须声明为 partial",
        messageFormat: "类 '{0}' 继承了 BuildToolsBase<TBuilder>，但未声明为 partial，无法生成工具调用方法",
        category: "CsTrees.MEAI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor CSTM002 { get; } = new(
        id: "CSTM002",
        title: "Builder 的多个 Catalog 中存在同名工厂方法",
        messageFormat: "方法 '{0}' 在 Builder 的多个 Catalog 中重复定义，无法生成同名工具方法；请重命名其中一个工厂方法",
        category: "CsTrees.MEAI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor CSTM003 { get; } = new(
        id: "CSTM003",
        title: "Catalog 工厂方法缺少 [Description]",
        messageFormat: "工厂方法 '{0}' 未标注 [Description]，生成的工具方法将缺少供 LLM 理解用途的描述",
        category: "CsTrees.MEAI",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
