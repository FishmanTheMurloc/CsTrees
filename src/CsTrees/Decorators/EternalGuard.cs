namespace CsTrees.Decorators;

/// <summary>
/// Continuously guard (with a condition) the execution of a child/subtree.
/// <para>
/// The eternal guard checks a condition prior to <em>every</em> tick of the child.
/// If at any time the condition fails, the child/subtree is invalidated and this
/// decorator returns <see cref="Status.Failure"/>.
/// </para>
/// <para>
/// This is stronger than a conventional guard which is only checked once before
/// any and all ticking of what follows the guard.
/// </para>
/// </summary>
public class EternalGuard : Decorator
{
    /// <summary>
    /// The condition function. Return <c>true</c> to allow the child to tick,
    /// <c>false</c> to abort.
    /// </summary>
    public Func<bool> Condition { get; }

    /// <summary>
    /// Create a new EternalGuard decorator.
    /// </summary>
    /// <param name="name">Name of the decorator.</param>
    /// <param name="child">The child behaviour to decorate.</param>
    /// <param name="condition">A condition function. Return <c>true</c> to allow the child to tick,
    /// <c>false</c> to abort.</param>
    public EternalGuard(string name, Behaviour child, Func<bool> condition) : base(name, child)
    {
        Condition = condition;
    }

    /// <summary>
    /// Conditionally tick the child. If the condition fails, stop immediately
    /// and return <see cref="Status.Failure"/>.
    /// </summary>
    public async override IAsyncEnumerable<Behaviour> Tick()
    {
        // Condition check
        if (!Condition())
        {
            Stop(Status.Failure);
            yield return this;
            yield break;
        }
        // Normal decorator behaviour
        await foreach (var node in base.Tick())
        {
            yield return node;
        }
    }

    /// <summary>
    /// Reflect the decorated child's status.
    /// <para>
    /// The update method is only triggered after the child's tick, which implies
    /// that the condition has already been checked and passed.
    /// </para>
    /// </summary>
    protected async override Task<Status> Update()
    {
        // Condition has already been checked and passed in Tick()
        return await Task.FromResult(Decorated.Status);
    }
}
