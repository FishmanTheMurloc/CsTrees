namespace CsTrees;

/// <summary>
/// Decorators are behaviours that manage a single child and provide common
/// modifications to the underlying child's behaviour (e.g. inverting the result).
/// <para>
/// The <see cref="Tick"/> flow for a decorator:
/// <list type="number">
///   <item>If not <see cref="Status.Running"/>, <see cref="Behaviour.Initialize"/> is called.</item>
///   <item>The <see cref="Decorated"/> child is ticked.</item>
///   <item><see cref="Behaviour.Update"/> is called to determine the decorator's status (may reflect or transform the child's status).</item>
///   <item>If the decorator's status is not <see cref="Status.Running"/>, <see cref="Stop"/> is called.</item>
/// </list>
/// </para>
/// <para>
/// Override <see cref="Behaviour.Update"/> to implement the decoration logic.
/// Override <see cref="Tick"/> only if you need to intercept before the child ticks
/// (e.g. <see cref="Decorators.EternalGuard"/>).
/// </para>
/// </summary>
public abstract class Decorator : Behaviour
{
    /// <summary>
    /// The child behaviour being decorated.
    /// </summary>
    public Behaviour Decorated { get; }

    /// <summary>
    /// Create a new decorator.
    /// </summary>
    /// <param name="name">Name of the decorator.</param>
    /// <param name="child">The child behaviour to decorate.</param>
    protected Decorator(string name, Behaviour child) : base(name)
    {
        Decorated = child;
        Children.Add(child);
        child.Parent = this;
    }

    /// <summary>
    /// Tick the decorated child, then apply the decorator's <see cref="Behaviour.Update"/>
    /// logic to determine the final status.
    /// </summary>
    public override async IAsyncEnumerable<Behaviour> Tick()
    {
        if (Status != Status.Running)
        {
            Initialize();
        }
        // Tick the child (including any subtree it may have)
        await foreach (var node in Decorated.Tick())
        {
            yield return node;
        }
        // Apply the decorator's logic
        var newStatus = await Update();
        if (!Enum.IsDefined(typeof(Status), newStatus))
        {
            newStatus = Status.Invalid;
        }
        if (newStatus != Status.Running)
        {
            Stop(newStatus);
        }
        Status = newStatus;
        yield return this;
    }

    /// <summary>
    /// Stop the decorator, handling priority interrupts and dangling children.
    /// <list type="bullet">
    ///   <item>If stopped with <see cref="Status.Invalid"/>, the decorated child is also stopped (priority interrupt).</item>
    ///   <item>If the decorated child is still <see cref="Status.Running"/> when the decorator finishes,
    ///     the child is stopped with <see cref="Status.Invalid"/> to prevent dangling states.</item>
    /// </list>
    /// </summary>
    public override void Stop(Status newStatus)
    {
        // Priority interrupt handling
        if (newStatus == Status.Invalid)
        {
            Decorated.Stop(newStatus);
        }
        // If the decorator returns a terminal status and the child is still running,
        // stop the child to prevent it from dangling
        if (Decorated.Status == Status.Running)
        {
            Decorated.Stop(Status.Invalid);
        }
        base.Stop(newStatus);
    }

    /// <summary>
    /// Get the tip of the decorated child's subtree if it is not
    /// <see cref="Status.Invalid"/>; otherwise return this decorator's tip.
    /// </summary>
    public override Behaviour? Tip()
    {
        return Decorated.Status != Status.Invalid ? Decorated.Tip() : base.Tip();
    }
}
