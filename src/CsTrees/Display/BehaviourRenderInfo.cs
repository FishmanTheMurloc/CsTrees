namespace CsTrees.Display;

/// <summary>
/// Structured information about a behaviour, produced by the traversal
/// logic and passed to <see cref="TreeRenderer.WriteBehaviour"/> for rendering.
/// <para>
/// All traversal decisions (visited filtering, tip detection, etc.) have
/// already been made — renderers only need to decide how to format this
/// information.
/// </para>
/// </summary>
public readonly struct BehaviourRenderInfo
{
    /// <summary>The behaviour being rendered.</summary>
    public Behaviour Behaviour { get; }

    /// <summary>
    /// Category string identifying the behaviour's type for symbol lookup.
    /// <para>
    /// Possible values:
    /// <list type="bullet">
    ///   <item><c>"parallel"</c></item>
    ///   <item><c>"decorator"</c></item>
    ///   <item><c>"sequence_with_memory"</c></item>
    ///   <item><c>"sequence_without_memory"</c></item>
    ///   <item><c>"selector_with_memory"</c></item>
    ///   <item><c>"selector_without_memory"</c></item>
    ///   <item><c>"behaviour"</c> (default for leaf behaviours)</item>
    /// </list>
    /// </para>
    /// </summary>
    public string BehaviourType { get; }

    /// <summary>Depth of this behaviour in the tree (0 = root).</summary>
    public int Depth { get; }

    /// <summary>
    /// Whether this behaviour is the current <see cref="Behaviour.Tip"/> of the tree.
    /// Renderers may highlight tip behaviours (e.g. bold).
    /// </summary>
    public bool IsTip { get; }

    /// <summary>
    /// Whether this behaviour was visited on the current tick.
    /// When <c>true</c>, status and feedback message should typically be shown.
    /// </summary>
    public bool Visited { get; }

    /// <summary>
    /// Whether this behaviour was running on the previous tick but not visited
    /// on the current tick. Renderers may show status without feedback
    /// for such behaviours.
    /// </summary>
    public bool PreviouslyRunning { get; }

    /// <summary>
    /// Whether this behaviour's children are collapsed (not rendered).
    /// When <c>true</c>, renderers may show a placeholder (e.g. "...")
    /// to indicate that the subtree exists but is not shown.
    /// </summary>
    public bool ChildrenCollapsed { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviourRenderInfo"/> struct.
    /// </summary>
    /// <param name="behaviour">The behaviour being rendered.</param>
    /// <param name="behaviourType">Category string identifying the behaviour's type for symbol lookup.</param>
    /// <param name="depth">Depth of this behaviour in the tree (0 = root).</param>
    /// <param name="isTip">Whether this behaviour is the current tip of the tree.</param>
    /// <param name="visited">Whether this behaviour was visited on the current tick.</param>
    /// <param name="previouslyRunning">Whether this behaviour was running on the previous tick but not visited on the current tick.</param>
    /// <param name="childrenCollapsed">Whether this behaviour's children are collapsed.</param>
    public BehaviourRenderInfo(
        Behaviour behaviour,
        string behaviourType,
        int depth,
        bool isTip,
        bool visited,
        bool previouslyRunning,
        bool childrenCollapsed)
    {
        Behaviour = behaviour;
        BehaviourType = behaviourType;
        Depth = depth;
        IsTip = isTip;
        Visited = visited;
        PreviouslyRunning = previouslyRunning;
        ChildrenCollapsed = childrenCollapsed;
    }
}
