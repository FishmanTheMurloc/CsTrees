namespace CsTrees.Decorators;

/// <summary>
/// A blocking conditional decorator.
/// <para>
/// Encapsulates a behaviour and waits for its status to flip to the desired state.
/// Returns <see cref="Status.Running"/> while waiting and <see cref="Status.Success"/>
/// when the flip occurs. This decorator will never return <see cref="Status.Failure"/>.
/// </para>
/// </summary>
public class Condition : Decorator
{
    /// <summary>
    /// The status the child must reach for this decorator to succeed.
    /// </summary>
    public Status SucceedStatus { get; }

    /// <summary>
    /// Create a new Condition decorator.
    /// </summary>
    /// <param name="name">Name of the decorator.</param>
    /// <param name="child">The child behaviour to decorate.</param>
    /// <param name="succeedStatus">The status the child must reach for this decorator to succeed.</param>
    public Condition(string name, Behaviour child, Status succeedStatus) : base(name, child)
    {
        SucceedStatus = succeedStatus;
    }

    /// <summary>
    /// Check if the condition has triggered, block otherwise.
    /// </summary>
    protected async override Task<Status> Update()
    {
        FeedbackMessage = $"'{Decorated.Name}' has status {Decorated.Status}, waiting for {SucceedStatus}";
        if (Decorated.Status == SucceedStatus)
            return await Task.FromResult(Status.Success);
        return await Task.FromResult(Status.Running);
    }
}
