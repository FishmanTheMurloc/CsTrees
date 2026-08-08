using CsTrees.Visitors;

namespace CsTrees;

/// <summary>
/// Abstract base class for all behaviour tree nodes.
/// <para>
/// A behaviour's lifecycle is managed by the <see cref="Tick"/> method:
/// <list type="number">
///   <item>If not <see cref="Status.Running"/>, <see cref="Initialize"/> is called.</item>
///   <item><see cref="Update"/> is called to determine the new status.</item>
///   <item>If the new status is not <see cref="Status.Running"/>, <see cref="Stop"/> is called.</item>
/// </list>
/// </para>
/// <para>
/// Override <see cref="Update"/> to implement the behaviour's logic.
/// Override <see cref="Initialize"/> to reset state before a new run.
/// Override <see cref="Terminate"/> to clean up when the behaviour is stopped.
/// </para>
/// </summary>
public abstract class Behaviour
{
    /// <summary>Unique identifier for this behaviour.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>Name of the behaviour.</summary>
    public string Name { get; }

    /// <summary>
    /// Current status of the behaviour.
    /// <para>
    /// Note: Do not set this directly in <see cref="Terminate"/> —
    /// it is handled automatically by <see cref="Stop"/> and <see cref="Tick"/>.
    /// </para>
    /// </summary>
    public Status Status { get; set; } = Status.Invalid;

    /// <summary>Parent behaviour (set when added to a composite).</summary>
    public Behaviour? Parent { get; set; }

    /// <summary>Child behaviours (populated only by composite nodes).</summary>
    public List<Behaviour> Children { get; } = [];

    /// <summary>Feedback message for debugging and introspection.</summary>
    public string FeedbackMessage { get; set; } = string.Empty;

    /// <summary>
    /// Create a new behaviour with the specified name.
    /// </summary>
    /// <param name="name">Name of the behaviour.</param>
    protected Behaviour(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Set up and verify infrastructure (e.g. middleware connections) is available.
    /// Called before the tree's first tick. Override for one-time configuration.
    /// </summary>
    public virtual void Setup() { }

    /// <summary>
    /// Called when the behaviour starts a new round of activity
    /// (i.e. when <see cref="Status"/> is not <see cref="Status.Running"/>).
    /// Override to reset variables before each run.
    /// <para>Note: This can be called more than once in the lifetime of a tree.</para>
    /// </summary>
    protected virtual void Initialize() { }

    /// <summary>
    /// The primary worker method. Override to implement the behaviour's logic
    /// and return its new status.
    /// <para>This method should be almost instantaneous and non-blocking.</para>
    /// </summary>
    protected abstract Task<Status> Update();

    /// <summary>
    /// Called when the behaviour is stopped. Override to clean up resources
    /// (e.g. cancel an external action, shut down temporary communication handles).
    /// <para>
    /// Do NOT set <see cref="Status"/> here — it is handled automatically.
    /// Use <paramref name="newStatus"/> purely for introspection.
    /// </para>
    /// </summary>
    /// <param name="newStatus">The status the behaviour is transitioning to.</param>
    protected virtual void Terminate(Status newStatus) { }

    /// <summary>
    /// Destroy setup infrastructure (the antithesis of <see cref="Setup"/>).
    /// Override for custom cleanup of infrastructure created in <see cref="Setup"/>.
    /// </summary>
    public virtual void Shutdown() { }

    /// <summary>
    /// Tick the behaviour, handling the lifecycle automatically:
    /// calls <see cref="Initialize"/>, <see cref="Update"/>, and <see cref="Stop"/>
    /// as appropriate, then yields itself.
    /// </summary>
    public virtual async IAsyncEnumerable<Behaviour> Tick()
    {
        if (Status != Status.Running)
        {
            Initialize();
        }
        var newStatus = await Update();
        if (newStatus != Status.Running)
        {
            Stop(newStatus);
        }
        Status = newStatus;
        yield return this;
    }

    /// <summary>
    /// Tick the behaviour without iterating step-by-step over children.
    /// Convenience method that runs <see cref="Tick"/> to completion.
    /// </summary>
    public async Task TickOnce()
    {
        await foreach (var _ in Tick()) { }
    }

    /// <summary>
    /// Stop the behaviour with the specified status.
    /// Calls <see cref="Terminate"/> and updates <see cref="Status"/>.
    /// </summary>
    /// <param name="newStatus">The status the behaviour is transitioning to.</param>
    public virtual void Stop(Status newStatus)
    {
        Terminate(newStatus);
        Status = newStatus;
    }

    /// <summary>
    /// Iterate over this behaviour and all its descendants (depth-first).
    /// </summary>
    /// <param name="directDescendants">
    /// If <c>true</c>, only yield children one step away from this behaviour.
    /// </param>
    public IEnumerable<Behaviour> Iterate(bool directDescendants = false)
    {
        foreach (var child in Children)
        {
            if (!directDescendants)
            {
                foreach (var node in child.Iterate())
                {
                    yield return node;
                }
            }
            else
            {
                yield return child;
            }
        }
        yield return this;
    }

    /// <summary>
    /// Get the deepest running node in this behaviour's subtree.
    /// Returns <c>null</c> if this behaviour's status is <see cref="Status.Invalid"/>.
    /// </summary>
    public virtual Behaviour? Tip()
    {
        return Status != Status.Invalid ? this : null;
    }

    /// <summary>
    /// Check if any ancestor has the specified name.
    /// </summary>
    public bool HasParentWithName(string name)
    {
        var current = this;
        while (current.Parent is not null)
        {
            if (current.Parent.Name == name)
                return true;
            current = current.Parent;
        }
        return false;
    }

    /// <summary>
    /// Check if any ancestor is of the specified type.
    /// </summary>
    public bool HasParentOfType<T>() where T : Behaviour
    {
        var current = this;
        while (current.Parent is not null)
        {
            if (current.Parent is T)
                return true;
            current = current.Parent;
        }
        return false;
    }

    /// <summary>
    /// Introspect on this behaviour with a visitor.
    /// <para>
    /// This enables external introspection into the behaviour. It gets used
    /// by the tree manager classes to collect information as ticking traverses a tree.
    /// </para>
    /// </summary>
    /// <param name="visitor">The visiting class, must have a <see cref="VisitorBase.Run"/> method.</param>
    public void Visit(VisitorBase visitor)
    {
        visitor.Run(this);
    }
}
