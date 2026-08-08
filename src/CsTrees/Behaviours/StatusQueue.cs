namespace CsTrees.Behaviours;

/// <summary>
/// Cycle through a specified queue of states.
/// <para>
/// When the queue is exhausted, it either cycles back to the beginning
/// or returns the <see cref="Eventually"/> status on every subsequent tick.
/// </para>
/// <para>
/// Note: This does not reset when the behaviour initialises.
/// </para>
/// <para>
/// Corresponds to <c>py_trees.behaviours.StatusQueue</c>.
/// </para>
/// </summary>
public class StatusQueue : Behaviour
{
    private Queue<Status> _currentQueue;

    /// <summary>The original queue of status values to cycle through.</summary>
    public IReadOnlyList<Status> Queue { get; }

    /// <summary>
    /// Status to use eventually, or <c>null</c> to re-cycle the sequence.
    /// </summary>
    public Status? Eventually { get; }

    /// <summary>
    /// Create a new StatusQueue behaviour.
    /// </summary>
    /// <param name="name">Name of the behaviour.</param>
    /// <param name="queue">List of status values to cycle through.</param>
    /// <param name="eventually">Status to use eventually, or <c>null</c> to re-cycle the sequence.</param>
    public StatusQueue(string name, IEnumerable<Status> queue, Status? eventually = null)
        : base(name)
    {
        Queue = queue.ToList().AsReadOnly();
        Eventually = eventually;
        _currentQueue = new Queue<Status>(Queue);
    }

    /// <summary>
    /// Pop from the queue or rotate / switch to eventual if the end has been reached.
    /// </summary>
    protected async override Task<Status> Update()
    {
        if (_currentQueue.Count > 0)
        {
            return await Task.FromResult(_currentQueue.Dequeue());
        }
        else if (Eventually is Status eventualStatus)
        {
            return await Task.FromResult(eventualStatus);
        }
        else
        {
            _currentQueue = new Queue<Status>(Queue);
            return await Task.FromResult(_currentQueue.Dequeue());
        }
    }
}
