namespace CsTrees.Behaviours;

/// <summary>
/// Block for a specified tick count.
/// <para>
/// A useful utility behaviour for demos and tests. Simply
/// ticks with <see cref="Status.Running"/> for
/// the specified number of ticks before returning the
/// requested completion status (<see cref="Status.Success"/>
/// or <see cref="Status.Failure"/>).
/// </para>
/// <para>
/// This behaviour will reset the tick counter when initialising.
/// </para>
/// <para>
/// Corresponds to <c>py_trees.behaviours.TickCounter</c>.
/// </para>
/// </summary>
public class TickCounter : Behaviour
{
    private int _counter;

    /// <summary>Number of ticks to run before completing.</summary>
    public int Duration { get; }

    /// <summary>Status to switch to once the counter has expired.</summary>
    public Status CompletionStatus { get; }

    /// <summary>
    /// Create a new TickCounter behaviour.
    /// </summary>
    /// <param name="name">Name of the behaviour.</param>
    /// <param name="duration">Number of ticks to run before completing.</param>
    /// <param name="completionStatus">Status to switch to once the counter has expired.</param>
    public TickCounter(string name, int duration, Status completionStatus) : base(name)
    {
        Duration = duration;
        CompletionStatus = completionStatus;
    }

    /// <summary>
    /// Reset the tick counter.
    /// </summary>
    protected override void Initialize()
    {
        _counter = 0;
    }

    /// <summary>
    /// Increment the tick counter and check to see if it should complete.
    /// </summary>
    protected async override Task<Status> Update()
    {
        _counter++;
        if (_counter <= Duration)
        {
            return await Task.FromResult(Status.Running);
        }
        else
        {
            return await Task.FromResult(CompletionStatus);
        }
    }
}
