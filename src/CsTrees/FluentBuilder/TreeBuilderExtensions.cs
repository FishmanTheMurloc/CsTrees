using CsTrees.Behaviours;
using CsTrees.Composites;
using CsTrees.Decorators;

namespace CsTrees.FluentBuilder;

/// <summary>
/// Extension methods for <see cref="TreeBuilder"/> providing convenient factory methods
/// for common behaviour tree node types.
/// </summary>
public static class TreeBuilderExtensions
{
    // ========================================================================
    // Composite nodes
    // ========================================================================

    /// <summary>
    /// Create a sequence node that executes children sequentially until one fails.
    /// </summary>
    public static TreeBuilder Sequence(this TreeBuilder builder, string name)
        => builder.PushComposite(children => new Sequence(name, children: children));

    /// <summary>
    /// Create a sequence with memory that resumes from the last running child.
    /// </summary>
    public static TreeBuilder SequenceWithMemory(this TreeBuilder builder, string name)
        => builder.PushComposite(children => new Sequence(name, memory: true, children: children));

    /// <summary>
    /// Create a selector node that tries children in order until one succeeds.
    /// </summary>
    public static TreeBuilder Selector(this TreeBuilder builder, string name)
        => builder.PushComposite(children => new Selector(name, children: children));

    /// <summary>
    /// Create a selector with memory that resumes from the last running child.
    /// </summary>
    public static TreeBuilder SelectorWithMemory(this TreeBuilder builder, string name)
        => builder.PushComposite(children => new Selector(name, memory: true, children: children));

    /// <summary>
    /// Create a parallel node that succeeds when all children succeed.
    /// </summary>
    public static TreeBuilder Parallel(this TreeBuilder builder, string name)
        => builder.PushComposite(children => new Composites.Parallel(name, new ParallelPolicy.SuccessOnAll(), children));

    /// <summary>
    /// Create a parallel node with a custom policy.
    /// </summary>
    public static TreeBuilder Parallel(this TreeBuilder builder, string name, ParallelPolicy policy)
        => builder.PushComposite(children => new Composites.Parallel(name, policy, children));

    // ========================================================================
    // Decorator nodes
    // ========================================================================

    /// <summary>
    /// Create an inverter that flips success/failure of its child.
    /// </summary>
    public static TreeBuilder Inverter(this TreeBuilder builder, string name)
        => builder.PushDecorator(child => new Inverter(name, child));

    /// <summary>
    /// Create a retry decorator that retries the child on failure.
    /// </summary>
    public static TreeBuilder Retry(this TreeBuilder builder, string name, int maxRetries)
        => builder.PushDecorator(child => new Retry(name, child, maxRetries));

    /// <summary>
    /// Create a repeat decorator that repeats the child multiple times.
    /// </summary>
    public static TreeBuilder Repeat(this TreeBuilder builder, string name, int count)
        => builder.PushDecorator(child => new Repeat(name, child, count));

    /// <summary>
    /// Create a timeout decorator that fails if the child takes too long.
    /// </summary>
    /// <param name="builder">The tree builder.</param>
    /// <param name="name">Node name.</param>
    /// <param name="durationSeconds">Timeout duration in seconds.</param>
    public static TreeBuilder Timeout(this TreeBuilder builder, string name, double durationSeconds)
        => builder.PushDecorator(child => new Decorators.Timeout(name, child, durationSeconds));

    /// <summary>
    /// Create a oneshot decorator that executes the child through to completion exactly once.
    /// </summary>
    /// <param name="builder">The tree builder.</param>
    /// <param name="name">Node name.</param>
    /// <param name="policy">Policy determining when the oneshot should activate.</param>
    public static TreeBuilder OneShot(this TreeBuilder builder, string name, OneShotPolicy policy = OneShotPolicy.OnCompletion)
        => builder.PushDecorator(child => new OneShot(name, child, policy));

    /// <summary>
    /// Create a condition decorator that blocks until the child reaches the specified status.
    /// </summary>
    /// <param name="builder">The tree builder.</param>
    /// <param name="name">Node name.</param>
    /// <param name="succeedStatus">The status the child must reach for this decorator to succeed.</param>
    public static TreeBuilder Condition(this TreeBuilder builder, string name, Status succeedStatus)
        => builder.PushDecorator(child => new Condition(name, child, succeedStatus));

    /// <summary>
    /// Create an eternal guard decorator that checks a condition before every tick of the child.
    /// </summary>
    /// <param name="builder">The tree builder.</param>
    /// <param name="name">Node name.</param>
    /// <param name="condition">A condition function. Return true to allow the child to tick, false to abort.</param>
    public static TreeBuilder EternalGuard(this TreeBuilder builder, string name, Func<bool> condition)
        => builder.PushDecorator(child => new EternalGuard(name, child, condition));

    // ========================================================================
    // Status mapping decorators
    // ========================================================================

    /// <summary>
    /// Create a decorator that reflects Running as Failure.
    /// </summary>
    public static TreeBuilder RunningIsFailure(this TreeBuilder builder, string name)
        => builder.PushDecorator(child => new RunningIsFailure(name, child));

    /// <summary>
    /// Create a decorator that reflects Running as Success.
    /// </summary>
    public static TreeBuilder RunningIsSuccess(this TreeBuilder builder, string name)
        => builder.PushDecorator(child => new RunningIsSuccess(name, child));

    /// <summary>
    /// Create a decorator that reflects Failure as Success.
    /// </summary>
    public static TreeBuilder FailureIsSuccess(this TreeBuilder builder, string name)
        => builder.PushDecorator(child => new FailureIsSuccess(name, child));

    /// <summary>
    /// Create a decorator that reflects Failure as Running.
    /// </summary>
    public static TreeBuilder FailureIsRunning(this TreeBuilder builder, string name)
        => builder.PushDecorator(child => new FailureIsRunning(name, child));

    /// <summary>
    /// Create a decorator that reflects Success as Failure.
    /// </summary>
    public static TreeBuilder SuccessIsFailure(this TreeBuilder builder, string name)
        => builder.PushDecorator(child => new SuccessIsFailure(name, child));

    /// <summary>
    /// Create a decorator that reflects Success as Running.
    /// </summary>
    public static TreeBuilder SuccessIsRunning(this TreeBuilder builder, string name)
        => builder.PushDecorator(child => new SuccessIsRunning(name, child));

    // ========================================================================
    // Leaf nodes
    // ========================================================================

    /// <summary>
    /// Add a success leaf node.
    /// </summary>
    public static TreeBuilder Success(this TreeBuilder builder, string name)
        => builder.Leaf(() => new Behaviours.Success(name));

    /// <summary>
    /// Add a failure leaf node.
    /// </summary>
    public static TreeBuilder Failure(this TreeBuilder builder, string name)
        => builder.Leaf(() => new Behaviours.Failure(name));

    /// <summary>
    /// Add a running leaf node.
    /// </summary>
    public static TreeBuilder Running(this TreeBuilder builder, string name)
        => builder.Leaf(() => new Behaviours.Running(name));

    /// <summary>
    /// Add a dummy leaf node that always returns Running (useful for crash testing).
    /// </summary>
    public static TreeBuilder Dummy(this TreeBuilder builder, string name)
        => builder.Leaf(() => new Behaviours.Dummy(name));

    /// <summary>
    /// Add a periodic leaf node that cycles through all statuses every N ticks.
    /// </summary>
    /// <param name="builder">The tree builder.</param>
    /// <param name="name">Node name.</param>
    /// <param name="period">Period value in ticks.</param>
    public static TreeBuilder Periodic(this TreeBuilder builder, string name, int period)
        => builder.Leaf(() => new Behaviours.Periodic(name, period));

    /// <summary>
    /// Add a status queue leaf node that cycles through a specified queue of statuses.
    /// </summary>
    /// <param name="builder">The tree builder.</param>
    /// <param name="name">Node name.</param>
    /// <param name="queue">List of status values to cycle through.</param>
    /// <param name="eventually">Status to use eventually, or null to re-cycle the sequence.</param>
    public static TreeBuilder StatusQueue(this TreeBuilder builder, string name, IEnumerable<Status> queue, Status? eventually = null)
        => builder.Leaf(() => new Behaviours.StatusQueue(name, queue, eventually));

    /// <summary>
    /// Add a leaf node that returns Success once every N ticks, Failure otherwise.
    /// </summary>
    /// <param name="builder">The tree builder.</param>
    /// <param name="name">Node name.</param>
    /// <param name="everyN">Trigger success on every N'th tick.</param>
    public static TreeBuilder SuccessEveryN(this TreeBuilder builder, string name, int everyN)
        => builder.Leaf(() => new Behaviours.SuccessEveryN(name, everyN));

    /// <summary>
    /// Add a tick counter leaf node that blocks for a specified tick count before completing.
    /// </summary>
    /// <param name="builder">The tree builder.</param>
    /// <param name="name">Node name.</param>
    /// <param name="duration">Number of ticks to run before completing.</param>
    /// <param name="completionStatus">Status to switch to once the counter has expired.</param>
    public static TreeBuilder TickCounter(this TreeBuilder builder, string name, int duration, Status completionStatus)
        => builder.Leaf(() => new Behaviours.TickCounter(name, duration, completionStatus));
}