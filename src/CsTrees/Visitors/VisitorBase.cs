namespace CsTrees.Visitors;

/// <summary>
/// Abstract base class for visitor types.
/// <para>
/// Visitors are entities that can be passed to a tree implementation
/// and used to either visit each and every behaviour in the tree,
/// or visit behaviours as the tree is traversed in an executing tick.
/// At each behaviour, the visitor runs its own method on the behaviour
/// to do as it wishes — logging, introspecting, etc.
/// </para>
/// <para>
/// Visitors should not modify the behaviours they visit.
/// </para>
/// </summary>
public abstract class VisitorBase
{
    /// <summary>
    /// Whether this visitor should visit only traversed nodes (<c>false</c>)
    /// or the entire tree (<c>true</c>).
    /// </summary>
    public bool Full { get; }

    /// <summary>
    /// Initialize the visitor base class.
    /// </summary>
    /// <param name="full">Flag to indicate whether it should visit only traversed nodes or the entire tree.</param>
    protected VisitorBase(bool full = false)
    {
        Full = full;
    }

    /// <summary>
    /// Override if any resetting of variables needs to be performed
    /// between ticks (i.e. visitations).
    /// </summary>
    public virtual void Initialise() { }

    /// <summary>
    /// Override if any work needs to be performed after ticks
    /// (i.e. showing data).
    /// </summary>
    public virtual void Finalise() { }

    /// <summary>
    /// Converse with the behaviour.
    /// <para>
    /// This method gets run as each behaviour is ticked. Override it to
    /// perform some activity — e.g. introspect the behaviour to
    /// store/process logging data for visualisations.
    /// </para>
    /// </summary>
    /// <param name="behaviour">Behaviour that is ticking.</param>
    public abstract void Run(Behaviour behaviour);
}
