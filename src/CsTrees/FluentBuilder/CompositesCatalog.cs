using CsTrees.Composites;

namespace CsTrees.FluentBuilder;

/// <summary>
/// 内置 Composite 行为目录，包含 Sequence、Selector、Parallel 等复合节点工厂方法。
/// 与 <see cref="IBehaviourCatalog"/> 配合使用，Source Generator 会自动为 TreeBuilder 子类生成构建方法。
/// </summary>
public class CompositesCatalog : IBehaviourCatalog
{
    /// <summary>
    /// 创建 Sequence 节点，按顺序执行子节点，仅当每个子节点返回 SUCCESS 时继续；
    /// 任一子节点返回 FAILURE 或 RUNNING 时停止并采纳该结果。
    /// </summary>
    /// <param name="name">节点名称。</param>
    /// <param name="memory">启用 memory 时，上一次 tick 处于 RUNNING 的子节点将作为起始点，跳过前面的子节点。</param>
    /// <param name="children">子行为集合。</param>
    public Composite Sequence(string name, bool memory, IEnumerable<Behaviour> children)
        => new Composites.Sequence(name, memory, children);

    /// <summary>
    /// 创建 Selector 节点，依次执行子节点直至其中一个返回 SUCCESS；
    /// 所有子节点均返回 FAILURE 时才返回 FAILURE。
    /// </summary>
    /// <param name="name">节点名称。</param>
    /// <param name="memory">启用 memory 时，上一次 tick 处于 RUNNING 的子节点将作为起始点，跳过更高优先级的检查。</param>
    /// <param name="children">子行为集合。</param>
    public Composite Selector(string name, bool memory, IEnumerable<Behaviour> children)
        => new Composites.Selector(name, memory, children);

    /// <summary>
    /// 创建 Parallel 节点，由 policy 决定成功条件。
    /// </summary>
    /// <param name="name">节点名称。</param>
    /// <param name="policy">并行策略，用于决定何时返回 SUCCESS。</param>
    /// <param name="children">子行为集合。</param>
    public Composite Parallel(string name, ParallelPolicy policy, IEnumerable<Behaviour> children)
        => new Composites.Parallel(name, policy, children);
}
