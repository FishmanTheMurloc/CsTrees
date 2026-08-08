using CsTrees.Visitors;

namespace CsTrees;

/// <summary>
/// Tree stewardship.
/// <para>
/// While a graph of connected behaviours and composites form a tree in their own right
/// (i.e. it can be initialised and ticked), it is usually convenient to wrap your tree
/// in another class to take care of a lot of the housework and provide some extra bells
/// and whistles that make your tree flourish.
/// </para>
/// <para>
/// Features:
/// <list type="bullet">
///   <item>Pre and post tick handlers to execute code automatically before and after a tick.</item>
///   <item>Visitor access to the parts of the tree that were traversed in a tick.</item>
///   <item>Continuous tick-tock support.</item>
/// </list>
/// </para>
/// </summary>
public class BehaviourTree
{
    /// <summary>Number of times the tree has been ticked.</summary>
    public int Count { get; private set; }

    /// <summary>Root node of the tree.</summary>
    public Behaviour Root { get; }

    /// <summary>Entities that visit traversed parts of the tree when it ticks.</summary>
    public List<VisitorBase> Visitors { get; } = [];

    /// <summary>Functions that run before the entire tree is ticked.</summary>
    public List<Action<BehaviourTree>> PreTickHandlers { get; } = [];

    /// <summary>Functions that run after the entire tree is ticked.</summary>
    public List<Action<BehaviourTree>> PostTickHandlers { get; } = [];

    /// <summary>
    /// Whether the tick-tock loop should be interrupted.
    /// </summary>
    public bool InterruptTickTocking { get; private set; }

    /// <summary>
    /// A callback invoked when the tree structure changes (e.g. subtree operations).
    /// </summary>
    public Action? TreeUpdateHandler { get; set; }

    /// <summary>
    /// Create a new behaviour tree with the specified root node.
    /// </summary>
    /// <param name="root">Root node of the tree.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="root"/> is null.</exception>
    public BehaviourTree(Behaviour root)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
    }

    /// <summary>
    /// Add a function to execute before the tree is ticked.
    /// <para>
    /// The function must have a single argument of type <see cref="BehaviourTree"/>.
    /// </para>
    /// </summary>
    /// <param name="handler">Function to execute before ticking.</param>
    public void AddPreTickHandler(Action<BehaviourTree> handler)
    {
        PreTickHandlers.Add(handler);
    }

    /// <summary>
    /// Add a function to execute after the tree has ticked.
    /// <para>
    /// The function must have a single argument of type <see cref="BehaviourTree"/>.
    /// </para>
    /// </summary>
    /// <param name="handler">Function to execute after ticking.</param>
    public void AddPostTickHandler(Action<BehaviourTree> handler)
    {
        PostTickHandlers.Add(handler);
    }

    /// <summary>
    /// Add a visitor that will be invoked on behaviours as the tree ticks.
    /// <para>
    /// Trees can run multiple visitors on each behaviour as they tick through a tree.
    /// </para>
    /// </summary>
    /// <param name="visitor">Sub-classed instance of a visitor.</param>
    public void AddVisitor(VisitorBase visitor)
    {
        Visitors.Add(visitor);
    }

    /// <summary>
    /// Crawl across the tree calling <see cref="Behaviour.Setup"/> on each behaviour.
    /// </summary>
    /// <param name="timeout">Time to wait in seconds (use <see cref="Timeout.Infinite"/> to block indefinitely).</param>
    /// <param name="visitor">Runnable entities on each node after its setup.</param>
    /// <exception cref="TimeoutException">Thrown when setup times out.</exception>
    public void Setup(double timeout = Timeout.Infinite, VisitorBase? visitor = null)
    {
        SetupTree(Root, timeout, visitor);
    }

    /// <summary>
    /// Crawl across the tree calling <see cref="Behaviour.Setup"/> on each behaviour.
    /// </summary>
    /// <param name="timeout">Timeout for setup operation.</param>
    /// <param name="visitor">Runnable entities on each node after its setup.</param>
    /// <exception cref="TimeoutException">Thrown when setup times out.</exception>
    public void Setup(TimeSpan timeout, VisitorBase? visitor = null)
    {
        SetupTree(Root, timeout.TotalSeconds, visitor);
    }

    internal static void SetupTree(Behaviour root, double timeout, VisitorBase? visitor)
    {
        if (timeout == Timeout.Infinite)
        {
            RunSetup(root, visitor);
            return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
        try
        {
            RunSetup(root, visitor, cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("tree setup timed out");
        }
    }

    static void RunSetup(Behaviour root, VisitorBase? visitor, CancellationToken cancellationToken = default)
    {
        visitor?.Initialise();
        foreach (var node in root.Iterate())
        {
            cancellationToken.ThrowIfCancellationRequested();
            node.Setup();
            if (visitor is not null)
                node.Visit(visitor);
        }
        visitor?.Finalise();
    }

    /// <summary>
    /// Tick the tree just once and run any handlers before and after the tick.
    /// </summary>
    /// <param name="preTickHandler">One-shot function to execute before ticking.</param>
    /// <param name="postTickHandler">One-shot function to execute after ticking.</param>
    public async Task Tick(Action<BehaviourTree>? preTickHandler = null, Action<BehaviourTree>? postTickHandler = null)
    {
        // Pre-tick handlers
        preTickHandler?.Invoke(this);
        foreach (var handler in PreTickHandlers)
            handler(this);

        // Initialise visitors
        foreach (var visitor in Visitors)
            visitor.Initialise();

        // Tick: iterate over traversed nodes
        var allNodes = new List<Behaviour>();
        await foreach (var node in Root.Tick())
        {
            allNodes.Add(node);
            // Visit non-full visitors on traversed nodes
            foreach (var visitor in Visitors.Where(v => !v.Full))
                node.Visit(visitor);
        }

        // Visit full visitors on entire tree
        foreach (var node in Root.Iterate())
        {
            foreach (var visitor in Visitors.Where(v => v.Full))
                node.Visit(visitor);
        }

        // Finalise visitors
        foreach (var visitor in Visitors)
            visitor.Finalise();

        // Post-tick handlers
        foreach (var handler in PostTickHandlers)
            handler(this);
        postTickHandler?.Invoke(this);

        Count++;
    }

    /// <summary>
    /// Tick continuously with the specified period.
    /// <para>
    /// Depending on the implementation, the period may be more or less accurately tracked.
    /// For example, if your tick time is greater than the specified period, the timing will overrun.
    /// </para>
    /// </summary>
    /// <param name="periodMs">Sleep this much between ticks (milliseconds).</param>
    /// <param name="numberOfIterations">Number of iterations to tick-tock (use <see cref="int.MinValue"/> for continuous).</param>
    /// <param name="stopOnTerminalState">If true, stops when the tree's status is Success or Failure.</param>
    /// <param name="preTickHandler">Function to execute before ticking.</param>
    /// <param name="postTickHandler">Function to execute after ticking.</param>
    public void TickTock(
        int periodMs,
        int numberOfIterations = int.MinValue,
        bool stopOnTerminalState = false,
        Action<BehaviourTree>? preTickHandler = null,
        Action<BehaviourTree>? postTickHandler = null)
    {
        var tickTocks = 0;
        var periodS = periodMs / 1000.0;

        while (!InterruptTickTocking
            && (tickTocks < numberOfIterations || numberOfIterations == int.MinValue))
        {
            var startTime = DateTime.UtcNow;
            Tick(preTickHandler, postTickHandler);
            var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
            var sleepTime = Math.Max(0.0, periodS - elapsed);
            System.Threading.Thread.Sleep((int)(sleepTime * 1000));
            tickTocks++;

            if (stopOnTerminalState && Root.Status != Status.Running)
                break;
        }

        InterruptTickTocking = false;
    }

    /// <summary>
    /// Get the tip of the tree.
    /// <para>
    /// Returns the deepest node (behaviour) that was running before subtree traversal
    /// reversed direction, or null if this behaviour's status is <see cref="Status.Invalid"/>.
    /// </para>
    /// </summary>
    /// <returns>The tip behaviour, or null.</returns>
    public Behaviour? Tip()
    {
        return Root.Tip();
    }

    /// <summary>
    /// Interrupt tick-tock if it is tick-tocking.
    /// <para>
    /// This will permit a currently executing tick to finish before interrupting the tick-tock.
    /// </para>
    /// </summary>
    public void Interrupt()
    {
        InterruptTickTocking = true;
    }

    /// <summary>
    /// Crawl across the tree, calling <see cref="Behaviour.Shutdown"/> on each behaviour.
    /// </summary>
    public void Shutdown()
    {
        foreach (var node in Root.Iterate())
            node.Shutdown();
    }
}

/// <summary>
/// Static helper methods for tree management.
/// </summary>
public static class Tree
{
    /// <summary>
    /// Crawl across a (sub)tree of behaviours calling <see cref="Behaviour.Setup"/> on each behaviour.
    /// <para>
    /// Visitors can optionally be provided to provide a node-by-node analysis
    /// on the result of each node's <see cref="Behaviour.Setup"/> before the next node's
    /// <see cref="Behaviour.Setup"/> is called.
    /// </para>
    /// </summary>
    /// <param name="root">Unmanaged (sub)tree root behaviour.</param>
    /// <param name="timeout">Time in seconds to wait (use <see cref="Timeout.Infinite"/> to block indefinitely).</param>
    /// <param name="visitor">Runnable entities on each node after its setup.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="root"/> is null.</exception>
    /// <exception cref="TimeoutException">Thrown when setup times out.</exception>
    public static void Setup(
        Behaviour root,
        double timeout = Timeout.Infinite,
        VisitorBase? visitor = null)
    {
        if (root is null)
            throw new ArgumentNullException(nameof(root));

        BehaviourTree.SetupTree(root, timeout, visitor);
    }

    /// <summary>
    /// Crawl across a (sub)tree of behaviours calling <see cref="Behaviour.Setup"/> on each behaviour.
    /// </summary>
    /// <param name="root">Unmanaged (sub)tree root behaviour.</param>
    /// <param name="timeout">Timeout for setup operation.</param>
    /// <param name="visitor">Runnable entities on each node after its setup.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="root"/> is null.</exception>
    /// <exception cref="TimeoutException">Thrown when setup times out.</exception>
    public static void Setup(
        Behaviour root,
        TimeSpan timeout,
        VisitorBase? visitor = null)
    {
        if (root is null)
            throw new ArgumentNullException(nameof(root));

        BehaviourTree.SetupTree(root, timeout.TotalSeconds, visitor);
    }
}
