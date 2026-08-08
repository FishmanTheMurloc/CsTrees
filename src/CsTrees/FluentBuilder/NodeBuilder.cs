namespace CsTrees.FluentBuilder;

/// <summary>
/// Abstract base class for building behaviour tree nodes.
/// </summary>
public abstract class NodeBuilder
{
    /// <summary>
    /// Build the actual behaviour node.
    /// </summary>
    public abstract Behaviour Build();
}

/// <summary>
/// Builder for leaf (non-composite) behaviours.
/// </summary>
public sealed class LeafBuilder : NodeBuilder
{
    private readonly Func<Behaviour> _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="LeafBuilder"/> class.
    /// </summary>
    /// <param name="factory">Factory function to create the leaf behaviour.</param>
    public LeafBuilder(Func<Behaviour> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <inheritdoc />
    public override Behaviour Build() => _factory();
}

/// <summary>
/// Builder for composite behaviours that can contain children.
/// </summary>
public sealed class CompositeBuilder : NodeBuilder
{
    private readonly Func<IEnumerable<Behaviour>, Composite> _factory;
    private readonly List<NodeBuilder> _children = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeBuilder"/> class.
    /// </summary>
    /// <param name="factory">Factory function to create the composite behaviour from its children.</param>
    public CompositeBuilder(Func<IEnumerable<Behaviour>, Composite> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Adds a child node builder to this composite.
    /// </summary>
    /// <param name="child">The child node builder to add.</param>
    public void AddChild(NodeBuilder child) => _children.Add(child ?? throw new ArgumentNullException(nameof(child)));

    /// <summary>
    /// Whether this composite builder has any children.
    /// </summary>
    internal bool HasChildren => _children.Count > 0;

    /// <summary>
    /// Remove the last added child. Used by <see cref="TreeBuilder.Preview"/>
    /// to undo temporary placeholder insertion.
    /// </summary>
    internal void RemoveLastChild() => _children.RemoveAt(_children.Count - 1);

    /// <inheritdoc />
    public override Behaviour Build()
    {
        var children = _children.Select(c => c.Build());
        return _factory(children);
    }
}

/// <summary>
/// Builder for decorator behaviours that wrap a single child.
/// </summary>
public sealed class DecoratorBuilder : NodeBuilder
{
    private readonly Func<Behaviour, Decorator> _factory;
    private NodeBuilder? _child;

    /// <summary>
    /// Initializes a new instance of the <see cref="DecoratorBuilder"/> class.
    /// </summary>
    /// <param name="factory">Factory function to create the decorator behaviour from its child.</param>
    public DecoratorBuilder(Func<Behaviour, Decorator> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Sets the child node builder for this decorator.
    /// </summary>
    /// <param name="child">The child node builder.</param>
    public void SetChild(NodeBuilder child) => _child = child ?? throw new ArgumentNullException(nameof(child));

    /// <summary>
    /// Whether this decorator builder has a child.
    /// </summary>
    internal bool HasChild => _child is not null;

    /// <summary>
    /// Clear the child reference. Used by <see cref="TreeBuilder.Preview"/>
    /// to undo temporary placeholder insertion.
    /// </summary>
    internal void ClearChild() => _child = null;

    /// <inheritdoc />
    public override Behaviour Build()
    {
        if (_child is null)
            throw new InvalidOperationException("Decorator must have a child node.");
        return _factory(_child.Build());
    }
}