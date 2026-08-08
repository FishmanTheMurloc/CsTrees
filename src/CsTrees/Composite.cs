namespace CsTrees;

/// <summary>
/// Abstract base class for composite behaviours that manage multiple children.
/// <para>
/// Composites direct the flow through the tree on a given tick.
/// They are the factories (Sequences, Parallels) and decision makers (Selectors).
/// </para>
/// <para>
/// Composites do not make use of <see cref="Update"/>; their logic is
/// implemented entirely in <see cref="Tick"/>.
/// </para>
/// </summary>
public abstract class Composite : Behaviour
{
    /// <summary>
    /// The child currently being ticked, or <c>null</c> if no child is active.
    /// </summary>
    public Behaviour? CurrentChild { get; set; }

    /// <summary>
    /// Create a new composite behaviour.
    /// </summary>
    /// <param name="name">Name of the composite behaviour.</param>
    /// <param name="children">List of children to add.</param>
    protected Composite(string name, IEnumerable<Behaviour>? children = null) : base(name)
    {
        if (children is not null)
        {
            foreach (var child in children)
                AddChild(child);
        }
    }

    /// <summary>
    /// Unused update method. Composites direct the flow via <see cref="Tick"/>,
    /// not <see cref="Update"/>.
    /// </summary>
    protected override Task<Status> Update() => Task.FromResult(Status.Invalid);

    /// <summary>
    /// Force composite subclasses to implement their own tick logic.
    /// </summary>
    public abstract override IAsyncEnumerable<Behaviour> Tick();

    /// <summary>
    /// Stop the composite, handling priority interrupts for children.
    /// When stopped with <see cref="Status.Invalid"/>, all non-Invalid children
    /// are also stopped.
    /// </summary>
    public override void Stop(Status newStatus)
    {
        // Priority interrupt handling
        if (newStatus == Status.Invalid)
        {
            CurrentChild = null;
            foreach (var child in Children)
            {
                if (child.Status != Status.Invalid)
                    child.Stop(newStatus);
            }
        }
        base.Stop(newStatus);
    }

    /// <summary>
    /// Get the tip of the current child's subtree, or this behaviour's tip
    /// if there is no current child.
    /// </summary>
    public override Behaviour? Tip()
    {
        return CurrentChild is not null ? CurrentChild.Tip() : base.Tip();
    }

    /// <summary>
    /// Add a child behaviour to this composite.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the child already has a parent.
    /// </exception>
    public void AddChild(Behaviour child)
    {
        if (child.Parent is not null)
            throw new InvalidOperationException(
                $"Behaviour '{child.Name}' already has parent '{child.Parent.Name}'");
        Children.Add(child);
        child.Parent = this;
    }

    /// <summary>
    /// Add multiple children. Returns <c>this</c> for fluent chaining.
    /// </summary>
    public Composite AddChildren(IEnumerable<Behaviour> children)
    {
        foreach (var child in children)
            AddChild(child);
        return this;
    }

    /// <summary>
    /// Remove a child behaviour. Returns the index at which the child was located.
    /// </summary>
    public int RemoveChild(Behaviour child)
    {
        if (CurrentChild is not null && ReferenceEquals(CurrentChild, child))
            CurrentChild = null;
        if (child.Status == Status.Running)
            child.Stop(Status.Invalid);
        int index = Children.IndexOf(child);
        Children.RemoveAt(index);
        child.Parent = null;
        return index;
    }

    /// <summary>
    /// Remove all children, stopping any that are running.
    /// </summary>
    public void RemoveAllChildren()
    {
        CurrentChild = null;
        foreach (var child in Children)
        {
            if (child.Status == Status.Running)
                child.Stop(Status.Invalid);
            child.Parent = null;
        }
        Children.Clear();
    }

    /// <summary>
    /// Replace a child with another behaviour at the same index.
    /// </summary>
    public void ReplaceChild(Behaviour child, Behaviour replacement)
    {
        int index = Children.IndexOf(child);
        RemoveChild(child);
        InsertChild(replacement, index);
    }

    /// <summary>
    /// Insert a child before all other children.
    /// </summary>
    public void PrependChild(Behaviour child)
    {
        Children.Insert(0, child);
        child.Parent = this;
    }

    /// <summary>
    /// Insert a child at the specified index.
    /// </summary>
    public void InsertChild(Behaviour child, int index)
    {
        Children.Insert(index, child);
        child.Parent = this;
    }
}
