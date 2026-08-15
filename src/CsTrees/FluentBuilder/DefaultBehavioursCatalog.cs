namespace CsTrees.FluentBuilder;

/// <summary>
/// 内置默认 Behaviour 行为目录，包含 Success、Failure、Running、Periodic 等叶子节点工厂方法。
/// 与 <see cref="IBehaviourCatalog"/> 配合使用，Source Generator 会自动为 TreeBuilder 子类生成构建方法。
/// </summary>
public class DefaultBehavioursCatalog : IBehaviourCatalog
{
    /// <summary>
    /// 创建叶子节点，始终返回 SUCCESS。
    /// </summary>
    /// <param name="name">节点名称。</param>
    public Behaviour Success(string name)
        => new Behaviours.Success(name);

    /// <summary>
    /// 创建叶子节点，始终返回 FAILURE。
    /// </summary>
    /// <param name="name">节点名称。</param>
    public Behaviour Failure(string name)
        => new Behaviours.Failure(name);

    /// <summary>
    /// 创建叶子节点，始终返回 RUNNING。
    /// </summary>
    /// <param name="name">节点名称。</param>
    public Behaviour Running(string name)
        => new Behaviours.Running(name);

    /// <summary>
    /// 创建测试用叶子节点，始终返回 RUNNING，用于危险操作的测试。
    /// </summary>
    /// <param name="name">节点名称。</param>
    public Behaviour Dummy(string name)
        => new Behaviours.Dummy(name);

    /// <summary>
    /// 创建周期性叶子节点，每 N 个 tick 在 RUNNING、SUCCESS、FAILURE 之间循环切换状态。
    /// </summary>
    /// <param name="name">节点名称。</param>
    /// <param name="n">每个状态持续的 tick 数。</param>
    public Behaviour Periodic(string name, int n)
        => new Behaviours.Periodic(name, n);

    /// <summary>
    /// 创建状态队列叶子节点，按指定队列依次循环返回各状态；队列耗尽后使用 eventually 或重新循环。
    /// </summary>
    /// <param name="name">节点名称。</param>
    /// <param name="queue">要循环的状态序列。</param>
    /// <param name="eventually">队列耗尽后的最终状态，为 null 时重新循环。</param>
    public Behaviour StatusQueue(string name, IEnumerable<Status> queue, Status? eventually = null)
        => new Behaviours.StatusQueue(name, queue, eventually);

    /// <summary>
    /// 创建周期性成功叶子节点，每 N 个 tick 返回一次 SUCCESS，其余 tick 返回 FAILURE。
    /// </summary>
    /// <param name="name">节点名称。</param>
    /// <param name="n">每 N 个 tick 触发一次 SUCCESS。</param>
    public Behaviour SuccessEveryN(string name, int n)
        => new Behaviours.SuccessEveryN(name, n);

    /// <summary>
    /// 创建 tick 计数器叶子节点，阻塞指定 tick 数后返回 completionStatus。
    /// </summary>
    /// <param name="name">节点名称。</param>
    /// <param name="duration">阻塞的 tick 数。</param>
    /// <param name="completionStatus">计数器到期后切换的目标状态。</param>
    public Behaviour TickCounter(string name, int duration, Status completionStatus)
        => new Behaviours.TickCounter(name, duration, completionStatus);
}
