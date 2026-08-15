using CsTrees.Decorators;

namespace CsTrees.FluentBuilder;

/// <summary>
/// 内置 Decorator 行为目录，包含 Inverter、Retry、Repeat、Timeout 等装饰节点工厂方法。
/// 与 <see cref="IBehaviourCatalog"/> 配合使用，Source Generator 会自动为 TreeBuilder 子类生成构建方法。
/// </summary>
public class DecoratorsCatalog : IBehaviourCatalog
{
    /// <summary>
    /// 创建 Inverter 装饰器，将子节点的 SUCCESS 和 FAILURE 结果翻转。
    /// </summary>
    public Decorator Inverter(string name, Behaviour child)
        => new Decorators.Inverter(name, child);

    /// <summary>
    /// 创建 Retry 装饰器，子节点返回 FAILURE 时继续重试，直到达到指定失败次数后最终返回 FAILURE。
    /// </summary>
    /// <param name="name">节点名称。</param>
    /// <param name="child">子行为。</param>
    /// <param name="numFailures">允许的最大失败次数。</param>
    public Decorator Retry(string name, Behaviour child, int numFailures)
        => new Decorators.Retry(name, child, numFailures);

    /// <summary>
    /// 创建 Repeat 装饰器，子节点返回 SUCCESS 时继续重复，直到达到指定成功次数后返回 SUCCESS；
    /// 子节点返回 FAILURE 时立即终止并返回 FAILURE。
    /// </summary>
    /// <param name="name">节点名称。</param>
    /// <param name="child">子行为。</param>
    /// <param name="numSuccess">需要达到的成功次数。</param>
    public Decorator Repeat(string name, Behaviour child, int numSuccess)
        => new Decorators.Repeat(name, child, numSuccess);

    /// <summary>
    /// 创建 Timeout 装饰器，为子节点设置超时；超时后终止子节点并返回 FAILURE。
    /// </summary>
    /// <param name="name">节点名称。</param>
    /// <param name="child">子行为。</param>
    /// <param name="duration">超时时间（秒）。</param>
    public Decorator Timeout(string name, Behaviour child, double duration)
        => new Decorators.Timeout(name, child, duration);

    /// <summary>
    /// 创建 OneShot 装饰器，确保子节点仅执行一次直至完成，之后直接返回最终状态不再执行子节点。
    /// </summary>
    /// <param name="name">节点名称。</param>
    /// <param name="child">子行为。</param>
    /// <param name="policy">决定 OneShot 何时激活的策略。</param>
    public Decorator OneShot(string name, Behaviour child, OneShotPolicy policy = OneShotPolicy.OnCompletion)
        => new Decorators.OneShot(name, child, policy);

    /// <summary>
    /// 创建 Condition 装饰器，阻塞等待子节点达到指定状态，达到后返回 SUCCESS，始终不返回 FAILURE。
    /// </summary>
    /// <param name="name">节点名称。</param>
    /// <param name="child">子行为。</param>
    /// <param name="status">子节点需要达到的目标状态。</param>
    public Decorator Condition(string name, Behaviour child, Status status)
        => new Decorators.Condition(name, child, status);

    /// <summary>
    /// 创建 EternalGuard 装饰器，在子节点每次 tick 前检查条件，条件不满足时中止子节点。
    /// </summary>
    /// <param name="name">节点名称。</param>
    /// <param name="child">子行为。</param>
    /// <param name="condition">条件函数，返回 true 允许执行，返回 false 中止。</param>
    public Decorator EternalGuard(string name, Behaviour child, System.Func<bool> condition)
        => new Decorators.EternalGuard(name, child, condition);

    // ========================================================================
    // 状态映射装饰器
    // ========================================================================

    /// <summary>
    /// 创建状态映射装饰器，将子节点的 RUNNING 映射为 FAILURE。
    /// </summary>
    public Decorator RunningIsFailure(string name, Behaviour child)
        => new Decorators.RunningIsFailure(name, child);

    /// <summary>
    /// 创建状态映射装饰器，将子节点的 RUNNING 映射为 SUCCESS。
    /// </summary>
    public Decorator RunningIsSuccess(string name, Behaviour child)
        => new Decorators.RunningIsSuccess(name, child);

    /// <summary>
    /// 创建状态映射装饰器，将子节点的 FAILURE 映射为 SUCCESS。
    /// </summary>
    public Decorator FailureIsSuccess(string name, Behaviour child)
        => new Decorators.FailureIsSuccess(name, child);

    /// <summary>
    /// 创建状态映射装饰器，将子节点的 FAILURE 映射为 RUNNING。
    /// </summary>
    public Decorator FailureIsRunning(string name, Behaviour child)
        => new Decorators.FailureIsRunning(name, child);

    /// <summary>
    /// 创建状态映射装饰器，将子节点的 SUCCESS 映射为 FAILURE。
    /// </summary>
    public Decorator SuccessIsFailure(string name, Behaviour child)
        => new Decorators.SuccessIsFailure(name, child);

    /// <summary>
    /// 创建状态映射装饰器，将子节点的 SUCCESS 映射为 RUNNING。
    /// </summary>
    public Decorator SuccessIsRunning(string name, Behaviour child)
        => new Decorators.SuccessIsRunning(name, child);
}
