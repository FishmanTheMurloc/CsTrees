namespace CsTrees.Composites;

/// <summary>
/// Sequences are the factory lines of behaviour trees.
/// <para>
/// A sequence progressively ticks over each child so long as each child returns
/// <see cref="Status.Success"/>. If any child returns <see cref="Status.Failure"/>
/// or <see cref="Status.Running"/>, the sequence halts and adopts the result
/// of that child. If it reaches the last child, it returns with that result.
/// </para>
/// <para>
/// When configured with <see cref="Memory"/> and a child returned
/// <see cref="Status.Running"/> on the previous tick, it will proceed directly
/// to that child, skipping preceding behaviours. With memory is useful for moving
/// through a long running series of tasks. Without memory is useful if you want
/// conditional guards to always be checked before the work.
/// </para>
/// </summary>
public class Sequence : Composite
{
    /// <summary>
    /// Whether the sequence should remember the last running child
    /// and resume from it on the next tick.
    /// </summary>
    public bool Memory { get; }

    /// <summary>
    /// Create a new Sequence behaviour.
    /// </summary>
    /// <param name="name">Name of the composite behaviour.</param>
    /// <param name="memory">If <c>true</c>, resume with the last running child on the next tick.</param>
    /// <param name="children">List of children to add.</param>
    public Sequence(string name, bool memory = false, IEnumerable<Behaviour>? children = null)
        : base(name, children)
    {
        Memory = memory;
    }

    /// <summary>
    /// Tick over the children sequentially until one fails or all succeed.
    /// </summary>
    public override async IAsyncEnumerable<Behaviour> Tick()
    {
        // Initialise
        int index = 0;
        if (Status != Status.Running)
        {
            CurrentChild = Children.Count > 0 ? Children[0] : null;
            foreach (var child in Children)
            {
                if (child.Status != Status.Invalid)
                    child.Stop(Status.Invalid);
            }
            Initialize();
        }
        else if (Memory && CurrentChild is not null)
        {
            index = Children.IndexOf(CurrentChild);
        }
        else if (!Memory)
        {
            CurrentChild = Children.Count > 0 ? Children[0] : null;
        }
        else
        {
            throw new InvalidOperationException("Sequence reached an unknown / invalid state");
        }

        // Nothing to do
        if (Children.Count == 0)
        {
            CurrentChild = null;
            Stop(Status.Success);
            yield return this;
            yield break;
        }

        // Actual work
        for (int i = index; i < Children.Count; i++)
        {
            var child = Children[i];
            await foreach (var node in child.Tick())
            {
                yield return node;
                if (ReferenceEquals(node, child) && node.Status != Status.Success)
                {
                    // Invalidate the remainder of the sequence (kill dangling runners)
                    if (!Memory)
                    {
                        for (int j = i + 1; j < Children.Count; j++)
                        {
                            if (Children[j].Status != Status.Invalid)
                                Children[j].Stop(Status.Invalid);
                        }
                    }

                    // Stop the sequence if a terminal (non-success) state was reached
                    if (node.Status != Status.Running)
                        Stop(node.Status);
                    else
                        Status = node.Status;

                    yield return this;
                    yield break;
                }
            }
            // Advance to next sibling
            if (i + 1 < Children.Count)
                CurrentChild = Children[i + 1];
        }

        Stop(Status.Success);
        yield return this;
    }
}
