namespace CsTrees.FluentBuilder;

/// <summary>
/// 标记 Behaviour 子类，指示 Source Generator 为其生成 TreeBuilder 扩展方法。
/// </summary>
/// <remarks>
/// 未标此特性的 Behaviour 子类不会自动生成 TreeBuilder 扩展方法。业务预设应通过
/// partial TreeBuilder 子类的 private 声明方法承载（由 SG 生成实例构建方法）；
/// 仅通用行为需要全局 TreeBuilder 扩展方法时才标此特性。
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GenerateTreeBuilderExtensionAttribute : Attribute
{
}
