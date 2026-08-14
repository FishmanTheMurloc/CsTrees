namespace CsTrees.FluentBuilder;

/// <summary>
/// 标记含 [BlackboardKey] 属性的 Behaviour 子类，指示 Source Generator 为其生成 TreeBuilder 扩展方法。
/// </summary>
/// <remarks>
/// 此特性专为简化 Blackboard 绑定而设计。生成的扩展方法会自动注入 blackboard 参数
/// 并将每个 [BlackboardKey] 端口暴露为可选的 <c>string? xxxKey = null</c> 参数。
///
/// 使用条件：类必须同时满足——
/// <list type="bullet">
///   <item>继承自 Behaviour</item>
///   <item>包含至少一个 [BlackboardKey] 标记的 BehaviourKeyAccess&lt;T&gt; 属性</item>
///   <item>声明为 partial</item>
///   <item>存在 private 构造函数（作为扩展方法的底层调用入口）</item>
/// </list>
/// 不满足条件的类会触发 CST005 或 CST006 警告。
///
/// 未标此特性的 Behaviour 子类不会自动生成 TreeBuilder 扩展方法。业务预设应通过
/// partial TreeBuilder 子类的 private 声明方法承载（由 SG 生成实例构建方法）；
/// 仅通用行为需要全局 TreeBuilder 扩展方法时才标此特性。
///
/// 不依赖 Blackboard 的 Composite（如 Parallel、Sequence）不适用此特性。
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GenerateTreeBuilderExtensionAttribute : Attribute
{
}
