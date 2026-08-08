namespace CsTrees.Composites;

/// <summary>
/// Selectors are the decision makers.
/// <para>
/// A selector executes each child in turn until one succeeds (returns
/// <see cref="Status.Running"/> or <see cref="Status.Success"/>),
/// or it runs out of children at which point it returns <see cref="Status.Failure"/>.
/// Children represent decreasingly lower priority paths.
/// </para>
/// <para>
/// When configured with <see cref="Memory"/>, higher-priority checks are skipped
/// when a child returned <see cref="Status.Running"/> on the previous tick — i.e.
/// once a priority is locked in, it runs to completion and can only be interrupted
/// if the selector itself is interrupted by higher priorities elsewhere in the tree.
/// </para>
/// </summary>
public class Selector : Composite
{
    /// <summary>
    /// Whether the selector should remember the last running child
    /// and resume from it on the next tick.
    /// </summary>
    public bool Memory { get; }

    /// <summary>
    /// Create a new Selector behaviour.
    /// </summary>
    /// <param name="name">Name of the composite behaviour.</param>
    /// <param name="memory">If <c>true</c>, resume with the last running child on the next tick.</param>
    /// <param name="children">List of children to add.</param>
    public Selector(string name, bool memory = false, IEnumerable<Behaviour>? children = null)
        : base(name, children)
    {
        Memory = memory;
    }

    /// <summary>
    /// Tick children in order until one succeeds or all fail.
    /// Implements priority-interrupt style handling amongst children.
    /// </summary>
    public override async IAsyncEnumerable<Behaviour> Tick()
    {
        // Initialise
        if (Status != Status.Running)
        {
            CurrentChild = Children.Count > 0 ? Children[0] : null;
            Initialize();
        }

        // Nothing to do
        if (Children.Count == 0)
        {
            CurrentChild = null;
            Stop(Status.Failure);
            yield return this;
            yield break;
        }

        // Starting point
        int index;
        if (Memory)
        {
            index = Children.IndexOf(CurrentChild!);
            // Clear out preceding statuses (helps visualization)
            for (int i = 0; i < index; i++)
            {
                if (Children[i].Status != Status.Invalid)
                    Children[i].Stop(Status.Invalid);
            }
        }
        else
        {
            index = 0;
        }

        // Actual work
        var previous = CurrentChild;
        for (int i = index; i < Children.Count; i++)
        {
            var child = Children[i];
            await foreach (var node in child.Tick())
            {
                yield return node;
                if (ReferenceEquals(node, child))
                {
                    if (node.Status == Status.Running || node.Status == Status.Success)
                    {
                        CurrentChild = child;
                        // Priority interrupt: invalidate lower-priority children
                        if (previous is null || !ReferenceEquals(previous, CurrentChild))
                        {
                            bool passed = false;
                            foreach (var c in Children)
                            {
                                if (passed && c.Status != Status.Invalid)
                                    c.Stop(Status.Invalid);
                                if (ReferenceEquals(c, CurrentChild))
                                    passed = true;
                            }
                        }

                        // Terminate the selector if a terminal state was reached
                        if (node.Status == Status.Success)
                            Stop(node.Status);
                        else
                            Status = node.Status;

                        yield return this;
                        yield break;
                    }
                    // FAILURE: inner foreach ends naturally, move to next child
                }
            }
        }

        // All children failed
        Stop(Status.Failure);
        CurrentChild = Children.Count > 0 ? Children[Children.Count - 1] : null;
        yield return this;
    }
}
