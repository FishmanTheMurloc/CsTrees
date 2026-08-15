namespace CsTrees.FluentBuilder;

/// <summary>
/// 默认的 TreeBuilder 实现，通过内置 Catalog 自动生成 Composite、Decorator、Behaviour 构建方法。
/// <para>
/// Source Generator 会扫描实现 <see cref="IBehaviourCatalog"/> 的字段，自动为每个工厂方法生成对应的构建方法。
/// 生成的方法支持 fluent 链式调用。
/// </para>
/// <example>
/// <code>
/// var tree = new DefaultTreeBuilder()
///     .Sequence("Root")
///         .Retry("Attempt", maxRetries: 3)
///             .Success("Action")
///         .End()
///     .End()
///     .Build();
/// </code>
/// </example>
/// </summary>
public partial class DefaultTreeBuilder : TreeBuilder<DefaultTreeBuilder>
{
    /// <summary>
    /// Composite 节点目录（Sequence、Selector、Parallel）。
    /// </summary>
    private readonly CompositesCatalog compositesCatalog = new();

    /// <summary>
    /// Decorator 节点目录（Retry、Repeat、Inverter、Timeout 等）。
    /// </summary>
    private readonly DecoratorsCatalog decoratorsCatalog = new();

    /// <summary>
    /// 默认 Behaviour 叶子节点目录（Success、Failure、Running、Periodic 等）。
    /// </summary>
    private readonly DefaultBehavioursCatalog defaultBehavioursCatalog = new();
}
