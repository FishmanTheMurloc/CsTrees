using CsTrees.Blackboard;
using CsTrees.Composites;

namespace CsTrees.Display;

/// <summary>
/// Static entry point for rendering behaviour trees.
/// <para>
/// Provides both a generic <see cref="RenderTree"/> method that works with
/// any <see cref="TreeRenderer"/>, and a convenience <see cref="AsciiTree"/>
/// method for the built-in ASCII format.
/// </para>
/// </summary>
public static class Display
{
    /// <summary>
    /// Render a behaviour tree using the specified renderer.
    /// <para>
    /// Handles tree traversal (including visited filtering, tip detection,
    /// and collapsed subtree handling) and feeds structured
    /// <see cref="BehaviourRenderInfo"/> to the renderer for each behaviour.
    /// </para>
    /// </summary>
    /// <param name="root">The root of the tree, or subtree, to render.</param>
    /// <param name="renderer">The renderer to produce output.</param>
    /// <param name="showOnlyVisited">
    /// If <c>true</c>, only behaviours visited on the current tick are fully
    /// expanded; unvisited subtrees are collapsed to a placeholder.
    /// </param>
    /// <param name="showStatus">
    /// If <c>true</c>, always show status and feedback message for every
    /// behaviour, not just visited ones.
    /// </param>
    /// <param name="visited">
    /// Dictionary of behaviour id and status pairs for behaviours visited
    /// on the current tick (typically from <see cref="Visitors.SnapshotVisitor.Visited"/>).
    /// </param>
    /// <param name="previouslyVisited">
    /// Dictionary of behaviour id and status pairs from the previous tick
    /// (typically from <see cref="Visitors.SnapshotVisitor.PreviouslyVisited"/>).
    /// </param>
    /// <param name="indent">The number of indentation levels to start at.</param>
    /// <returns>The rendered output string.</returns>
    public static string RenderTree(
        Behaviour root,
        TreeRenderer renderer,
        bool showOnlyVisited = false,
        bool showStatus = false,
        Dictionary<Guid, Status>? visited = null,
        Dictionary<Guid, Status>? previouslyVisited = null,
        int indent = 0)
    {
        var _visited = visited ?? new Dictionary<Guid, Status>();
        var _previouslyVisited = previouslyVisited ?? new Dictionary<Guid, Status>();

        var tip = root.Tip();
        var tipId = tip?.Id ?? Guid.Empty;

        renderer.ShowStatus = showStatus;
        renderer.Begin();
        RenderBehaviourRecursive(root, indent, showOnlyVisited, _visited, _previouslyVisited, tipId, renderer);
        renderer.End();

        return renderer.GetResult();
    }

    /// <summary>
    /// Render a behaviour tree as ASCII text.
    /// <para>
    /// Convenience method that creates an <see cref="AsciiTreeRenderer"/>
    /// and delegates to <see cref="RenderTree"/>.
    /// </para>
    /// </summary>
    /// <param name="root">The root of the tree, or subtree, to render.</param>
    /// <param name="showOnlyVisited">Show only visited behaviours and collapse unvisited subtrees.</param>
    /// <param name="showStatus">Always show status and feedback message for every element.</param>
    /// <param name="visited">
    /// Dictionary of behaviour id and status pairs for behaviours visited
    /// on the current tick.
    /// </param>
    /// <param name="previouslyVisited">
    /// Dictionary of behaviour id and status pairs from the previous tick.
    /// </param>
    /// <param name="indent">The number of indentation levels to start at.</param>
    /// <returns>An ASCII tree string.</returns>
    public static string AsciiTree(
        Behaviour root,
        bool showOnlyVisited = false,
        bool showStatus = false,
        Dictionary<Guid, Status>? visited = null,
        Dictionary<Guid, Status>? previouslyVisited = null,
        int indent = 0)
    {
        return RenderTree(
            root,
            new AsciiTreeRenderer(),
            showOnlyVisited,
            showStatus,
            visited,
            previouslyVisited,
            indent);
    }

    /// <summary>
    /// Render a blackboard using the specified renderer.
    /// <para>
    /// Iterates over the provided items and feeds each to the renderer.
    /// Callers are responsible for obtaining and filtering items from the blackboard.
    /// </para>
    /// </summary>
    /// <param name="items">The blackboard items to render (typically from <see cref="Blackboard.Blackboard.GetItems"/>).</param>
    /// <param name="renderer">The renderer to produce output.</param>
    /// <returns>The rendered output string.</returns>
    public static string RenderBlackboard(
        IEnumerable<BlackboardItem> items,
        BlackboardRenderer renderer)
    {
        renderer.Begin();
        foreach (var item in items)
            renderer.WriteItem(item);
        renderer.End();
        return renderer.GetResult();
    }

    /// <summary>
    /// Render a blackboard as ASCII text.
    /// <para>
    /// Convenience method that creates an <see cref="AsciiBlackboardRenderer"/>
    /// and delegates to <see cref="RenderBlackboard"/>.
    /// </para>
    /// </summary>
    /// <param name="items">The blackboard items to render (typically from <see cref="Blackboard.Blackboard.GetItems"/>).</param>
    /// <param name="symbols">Optional symbol configuration. Defaults to <see cref="BlackboardSymbols.Default"/>.</param>
    /// <returns>An ASCII blackboard string.</returns>
    public static string AsciiBlackboard(
        IEnumerable<BlackboardItem> items,
        BlackboardSymbols? symbols = null)
    {
        return RenderBlackboard(items, new AsciiBlackboardRenderer(symbols));
    }

    /// <summary>
    /// Render an activity stream using the specified renderer.
    /// <para>
    /// Iterates over the provided items and feeds each to the renderer.
    /// Callers are responsible for obtaining items from the activity stream
    /// (typically via <see cref="Blackboard.ActivityStream"/>).
    /// </para>
    /// </summary>
    /// <param name="items">The activity items to render (typically from <see cref="ActivityStream.Data"/>).</param>
    /// <param name="renderer">The renderer to produce output.</param>
    /// <param name="showTitle">Whether to include the title line in the output. Default is <c>true</c>.</param>
    /// <param name="indent">The number of indentation levels to start at. Default is 0.</param>
    /// <returns>The rendered output string.</returns>
    public static string RenderActivityStream(
        IEnumerable<ActivityItem> items,
        ActivityStreamRenderer renderer,
        bool showTitle = true,
        int indent = 0)
    {
        renderer.ShowTitle = showTitle;
        renderer.Indent = indent;
        renderer.Begin();
        foreach (var item in items)
            renderer.WriteItem(item);
        renderer.End();
        return renderer.GetResult();
    }

    /// <summary>
    /// Render an activity stream as ASCII text.
    /// <para>
    /// Convenience method that creates an <see cref="AsciiActivityStreamRenderer"/>
    /// and delegates to <see cref="RenderActivityStream"/>.
    /// </para>
    /// </summary>
    /// <param name="items">The activity items to render (typically from <see cref="ActivityStream.Data"/>).</param>
    /// <param name="symbols">Optional symbol configuration. Defaults to <see cref="ActivityStreamSymbols.Default"/>.</param>
    /// <param name="showTitle">Whether to include the title line in the output. Default is <c>true</c>.</param>
    /// <param name="indent">The number of indentation levels to start at. Default is 0.</param>
    /// <returns>An ASCII activity stream string.</returns>
    public static string AsciiActivityStream(
        IEnumerable<ActivityItem> items,
        ActivityStreamSymbols? symbols = null,
        bool showTitle = true,
        int indent = 0)
    {
        return RenderActivityStream(items, new AsciiActivityStreamRenderer(symbols), showTitle, indent);
    }

    /// <summary>
    /// Determine the behaviour type string for a behaviour.
    /// Used to populate <see cref="BehaviourRenderInfo.BehaviourType"/>.
    /// </summary>
    internal static string GetBehaviourType(Behaviour b)
    {
        if (b is Composites.Parallel)
            return "parallel";
        if (b is Decorator)
            return "decorator";
        if (b is Sequence sequence)
            return sequence.Memory ? "sequence_with_memory" : "sequence_without_memory";
        if (b is Selector selector)
            return selector.Memory ? "selector_with_memory" : "selector_without_memory";
        return "behaviour";
    }

    private static void RenderBehaviourRecursive(
        Behaviour behaviour,
        int depth,
        bool showOnlyVisited,
        Dictionary<Guid, Status> visited,
        Dictionary<Guid, Status> previouslyVisited,
        Guid tipId,
        TreeRenderer renderer)
    {
        var isVisited = visited.ContainsKey(behaviour.Id);
        var wasPreviouslyRunning =
            previouslyVisited.TryGetValue(behaviour.Id, out var prevStatus)
            && !isVisited
            && prevStatus == Status.Running;
        var isTip = behaviour.Id == tipId;
        var hasChildren = behaviour.Children.Count > 0;
        var childrenCollapsed = showOnlyVisited && hasChildren && !isVisited;

        var info = new BehaviourRenderInfo(
            behaviour: behaviour,
            behaviourType: GetBehaviourType(behaviour),
            depth: depth,
            isTip: isTip,
            visited: isVisited,
            previouslyRunning: wasPreviouslyRunning,
            childrenCollapsed: childrenCollapsed);

        renderer.WriteBehaviour(info);

        if (hasChildren && !childrenCollapsed)
        {
            foreach (var child in behaviour.Children)
            {
                RenderBehaviourRecursive(child, depth + 1, showOnlyVisited, visited, previouslyVisited, tipId, renderer);
            }
        }
    }
}
