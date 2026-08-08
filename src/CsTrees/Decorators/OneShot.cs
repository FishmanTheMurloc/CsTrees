namespace CsTrees.Decorators;

/// <summary>
/// Policy rules for <see cref="OneShot"/> to determine when the oneshot activates.
/// </summary>
public enum OneShotPolicy
{
    /// <summary>
    /// Activate when the child ticks to completion (success or failure).
    /// </summary>
    OnCompletion,
    /// <summary>
    /// Activate only when the child succeeds (failures are rerun).
    /// </summary>
    OnSuccessfulCompletion
}

/// <summary>
/// A decorator that implements the oneshot pattern.
/// <para>
/// Ticks the child through to completion exactly once. While doing so, it returns
/// with the same status as its child. Thereafter, it returns with the final status
/// of the child (i.e. it "bounces" without re-ticking the child).
/// </para>
/// </summary>
public class OneShot : Decorator
{
    /// <summary>
    /// The policy determining when the oneshot should activate.
    /// </summary>
    public OneShotPolicy Policy { get; }

    private Status? _finalStatus;

    /// <summary>
    /// Create a new OneShot decorator.
    /// </summary>
    /// <param name="name">Name of the decorator.</param>
    /// <param name="child">The child behaviour to decorate.</param>
    /// <param name="policy">Policy determining when the oneshot should activate.</param>
    public OneShot(string name, Behaviour child, OneShotPolicy policy) : base(name, child)
    {
        Policy = policy;
    }

    /// <summary>
    /// Bounce if the child has already successfully completed.
    /// </summary>
    protected async override Task<Status> Update()
    {
        if (_finalStatus.HasValue)
        {
            FeedbackMessage = "oneshot completed, bouncing";
            return await Task.FromResult(_finalStatus.Value);
        }
        return await Task.FromResult(Decorated.Status);
    }

    /// <summary>
    /// Tick the child or bounce back with the original status if already completed.
    /// </summary>
    public async override IAsyncEnumerable<Behaviour> Tick()
    {
        if (_finalStatus.HasValue)
        {
            // Already completed — bounce without ticking the child
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
        else
        {
            // Tick the child normally via Decorator.Tick()
            await foreach (var node in base.Tick())
            {
                yield return node;
            }
        }
    }

    /// <summary>
    /// Register that the behaviour has gone through to completion.
    /// In future ticks, it will block entry to the child and just return the original status result.
    /// </summary>
    protected override void Terminate(Status newStatus)
    {
        if (!_finalStatus.HasValue && IsCompletionStatus(newStatus))
        {
            FeedbackMessage = "oneshot completed";
            _finalStatus = newStatus;
        }
    }

    private bool IsCompletionStatus(Status status)
    {
        return Policy switch
        {
            OneShotPolicy.OnCompletion => status == Status.Success || status == Status.Failure,
            OneShotPolicy.OnSuccessfulCompletion => status == Status.Success,
            _ => false
        };
    }
}
