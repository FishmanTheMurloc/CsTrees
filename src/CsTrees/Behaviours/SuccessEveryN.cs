namespace CsTrees.Behaviours;

/// <summary>
/// Non-blocking, periodic success.
/// <para>
/// This behaviour updates its status with <see cref="Status.Success"/>
/// once every N ticks, <see cref="Status.Failure"/> otherwise.
/// </para>
/// <para>
/// Tip: Use with decorators to change the status value as desired, e.g.
/// <see cref="Decorators.Inverter"/>.
/// </para>
/// <para>
/// Corresponds to <c>py_trees.behaviours.SuccessEveryN</c>.
/// </para>
/// </summary>
public class SuccessEveryN : Behaviour
{
    private int _count;

    /// <summary>Trigger success on every N'th tick.</summary>
    public int EveryN { get; }

    /// <summary>
    /// Create a new SuccessEveryN behaviour.
    /// </summary>
    /// <param name="name">Name of the behaviour.</param>
    /// <param name="n">Trigger success on every N'th tick.</param>
    public SuccessEveryN(string name, int n) : base(name)
    {
        EveryN = n;
    }

    /// <summary>
    /// Increment the counter and decide on success/failure from that.
    /// </summary>
    protected async override Task<Status> Update()
    {
        _count++;
        if (_count % EveryN == 0)
        {
            FeedbackMessage = "now";
            return await Task.FromResult(Status.Success);
        }
        else
        {
            FeedbackMessage = "not yet";
            return await Task.FromResult(Status.Failure);
        }
    }
}
