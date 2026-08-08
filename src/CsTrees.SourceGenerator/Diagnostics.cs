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
}
