using Microsoft.CodeAnalysis;

namespace CsTrees.SourceGenerator;

internal static class Diagnostics
{
    public static DiagnosticDescriptor CST001 { get; } = new(
        id: "CST001",
        title: "[BlackboardKey] 属性所在的类必须声明为 partial",
        messageFormat: "类 '{0}' 包含 [BlackboardKey] 属性，但未声明为 partial",
        category: "CsTrees.Blackboard",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor CST002 { get; } = new(
        id: "CST002",
        title: "[BlackboardKey] 属性所在的类必须继承自 Behaviour",
        messageFormat: "类 '{0}' 包含 [BlackboardKey] 属性，但未继承自 CsTrees.Behaviour",
        category: "CsTrees.Blackboard",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor CST003 { get; } = new(
        id: "CST003",
        title: "[BlackboardKey] 属性的类型必须为 BehaviourKeyAccess<T>",
        messageFormat: "属性 '{0}' 标记了 [BlackboardKey]，但其类型 '{1}' 不是 BehaviourKeyAccess<T>",
        category: "CsTrees.Blackboard",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor CST004 { get; } = new(
        id: "CST004",
        title: "实现 IBehaviourCatalog 的 TreeBuilder 子类必须声明为 partial",
        messageFormat: "类 '{0}' 实现了 IBehaviourCatalog<TCatalog> 但未声明为 partial，无法生成构建方法",
        category: "CsTrees.FluentBuilder",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor CST005 { get; } = new(
        id: "CST005",
        title: "[GenerateTreeBuilderExtension] 需标注在含 [BlackboardKey] 的 Behaviour 子类上",
        messageFormat: "类 '{0}' 标注了 [GenerateTreeBuilderExtension]，但未包含 [BlackboardKey] 属性或未继承 Behaviour，不会生成 TreeBuilder 扩展方法",
        category: "CsTrees.FluentBuilder",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor CST006 { get; } = new(
        id: "CST006",
        title: "[GenerateTreeBuilderExtension] 需提供 private 构造函数以生成扩展方法",
        messageFormat: "类 '{0}' 标注了 [GenerateTreeBuilderExtension]，但未提供 private 构造函数，无法生成 TreeBuilder 扩展方法",
        category: "CsTrees.FluentBuilder",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
