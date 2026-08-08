namespace CsTrees.Visitors;

/// <summary>
/// Creates a snapshot of the tree state (behaviour statuses only).
/// <para>
/// Visits the ticked part of a tree, checking off the status against the set of status
/// results recorded in the previous tick. If there has been a change, it flags it.
/// This is useful for determining when to trigger, e.g. logging.
/// </para>
/// </summary>
public class SnapshotVisitor : VisitorBase
{
    /// <summary>
    /// Flagged if there is a difference in the visited path or
    /// <see cref="Status"/> of any behaviour on the path.
    /// </summary>
    public bool Changed { get; private set; }

    /// <summary>
    /// Dictionary of behaviour id (<see cref="Guid"/>) and status
    /// (<see cref="Status"/>) pairs from the current tick.
    /// </summary>
    public Dictionary<Guid, Status> Visited { get; private set; } = [];

    /// <summary>
    /// Dictionary of behaviour id (<see cref="Guid"/>) and status
    /// (<see cref="Status"/>) pairs from the previous tick.
    /// </summary>
    public Dictionary<Guid, Status> PreviouslyVisited { get; private set; } = [];

    /// <summary>
    /// Initialize the SnapshotVisitor.
    /// </summary>
    public SnapshotVisitor() : base(full: false) { }

    /// <summary>
    /// Store the last snapshot for comparison with the next incoming snapshot.
    /// <para>
    /// This should get called before a tree ticks.
    /// </para>
    /// </summary>
    public override void Initialise()
    {
        Changed = false;
        PreviouslyVisited = Visited;
        Visited = [];
    }

    /// <summary>
    /// Catch the id, status and store it.
    /// <para>
    /// Additionally flag <see cref="Changed"/> if the status differs from
    /// the previous tick or if the behaviour was not visited previously.
    /// </para>
    /// </summary>
    /// <param name="behaviour">Behaviour that is ticking.</param>
    public override void Run(Behaviour behaviour)
    {
        Visited[behaviour.Id] = behaviour.Status;
        if (!PreviouslyVisited.TryGetValue(behaviour.Id, out var previousStatus)
            || Visited[behaviour.Id] != previousStatus)
        {
            Changed = true;
        }
    }
}
