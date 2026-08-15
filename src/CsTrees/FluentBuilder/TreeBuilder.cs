namespace CsTrees.FluentBuilder;

using CsTrees.Behaviours;
using Blackboard = CsTrees.Blackboard.Blackboard;

/// <summary>
/// Fluent builder for constructing behaviour trees.
/// Uses a stack-based approach to manage nested composite, decorator, and blackboard scopes.
/// </summary>
/// <remarks>
/// <para>
/// Example usage:
/// <code>
/// var tree = new TreeBuilder()
///     .Sequence("Main")
///         .WithBlackboard(bb1)
///             .Leaf(() => new Success("Action1"))
///         .End()
///         .Leaf(() => new Success("Action2"))
///     .End()
///     .Build();
/// </code>
/// </para>
/// </remarks>
/// <typeparam name="TBuilder">The derived builder type for fluent chaining.</typeparam>
public class TreeBuilder<TBuilder>
    where TBuilder : TreeBuilder<TBuilder>
{
    /// <summary>
    /// Base class for frames on the builder stack.
    /// </summary>
    private abstract class Frame { }

    /// <summary>
    /// Frame for composite node builders.
    /// </summary>
    private sealed class CompositeFrame : Frame
    {
        public CompositeBuilder Builder { get; set; } = null!;
    }

    /// <summary>
    /// Frame for decorator node builders.
    /// </summary>
    private sealed class DecoratorFrame : Frame
    {
        public DecoratorBuilder Builder { get; set; } = null!;
    }

    /// <summary>
    /// Frame for blackboard scope.
    /// </summary>
    private sealed class BlackboardFrame : Frame
    {
        public Blackboard Blackboard { get; set; } = null!;
    }

    private readonly Stack<Frame> _frameStack = new();
    private NodeBuilder? _root;

    /// <summary>
    /// Protected constructor to allow subclassing for domain-specific builders.
    /// </summary>
    protected TreeBuilder() { }

    /// <summary>
    /// Get the current blackboard from the nearest blackboard frame in the stack.
    /// </summary>
    private Blackboard? GetCurrentBlackboard()
    {
        foreach (var frame in _frameStack)
        {
            if (frame is BlackboardFrame bbFrame)
                return bbFrame.Blackboard;
        }
        return null;
    }

    /// <summary>
    /// Get the current node builder from the nearest composite or decorator frame.
    /// </summary>
    private NodeBuilder? GetCurrentNodeBuilder()
    {
        foreach (var frame in _frameStack)
        {
            if (frame is CompositeFrame compositeFrame)
                return compositeFrame.Builder;
            if (frame is DecoratorFrame decoratorFrame)
                return decoratorFrame.Builder;
        }
        return null;
    }

    /// <summary>
    /// Cast <c>this</c> to the derived builder type for fluent chaining.
    /// </summary>
    protected TBuilder Self => (TBuilder)this;

    /// <summary>
    /// Push a composite node onto the builder stack.
    /// </summary>
    /// <param name="factory">Factory function that creates the composite from its children.</param>
    /// <returns>This builder for method chaining.</returns>
    public TBuilder PushComposite(Func<IEnumerable<Behaviour>, Composite> factory)
    {
        var builder = new CompositeBuilder(factory);

        var parent = GetCurrentNodeBuilder();
        if (parent is CompositeBuilder compositeParent)
            compositeParent.AddChild(builder);
        else if (parent is DecoratorBuilder decoratorParent)
            decoratorParent.SetChild(builder);

        _frameStack.Push(new CompositeFrame { Builder = builder });
        return Self;
    }

    /// <summary>
    /// Push a decorator node onto the builder stack.
    /// The next node added will become the decorator's child.
    /// </summary>
    /// <param name="factory">Factory function that creates the decorator from its child.</param>
    /// <returns>This builder for method chaining.</returns>
    public TBuilder PushDecorator(Func<Behaviour, Decorator> factory)
    {
        var builder = new DecoratorBuilder(factory);

        var parent = GetCurrentNodeBuilder();
        if (parent is CompositeBuilder compositeParent)
            compositeParent.AddChild(builder);
        else if (parent is DecoratorBuilder decoratorParent)
            decoratorParent.SetChild(builder);

        _frameStack.Push(new DecoratorFrame { Builder = builder });
        return Self;
    }

    /// <summary>
    /// Push a blackboard scope onto the builder stack.
    /// Subsequent nodes will be associated with this blackboard.
    /// </summary>
    /// <param name="blackboard">The blackboard to use for this scope.</param>
    /// <returns>This builder for method chaining.</returns>
    public TBuilder WithBlackboard(Blackboard blackboard)
    {
        if (blackboard is null)
            throw new ArgumentNullException(nameof(blackboard));

        _frameStack.Push(new BlackboardFrame { Blackboard = blackboard });
        return Self;
    }

    /// <summary>
    /// Add a leaf (non-composite) behaviour node.
    /// </summary>
    /// <param name="factory">Factory function that creates the behaviour.</param>
    /// <returns>This builder for method chaining.</returns>
    public TBuilder Leaf(Func<Behaviour> factory)
    {
        var builder = new LeafBuilder(factory);

        var parent = GetCurrentNodeBuilder();
        if (parent is null)
            throw new InvalidOperationException("Cannot add leaf node at root level. Use a composite or decorator as the root.");

        if (parent is CompositeBuilder compositeParent)
            compositeParent.AddChild(builder);
        else if (parent is DecoratorBuilder decoratorParent)
            decoratorParent.SetChild(builder);
        else
            throw new InvalidOperationException($"Cannot add leaf child to {parent.GetType().Name}");

        return Self;
    }

    /// <summary>
    /// Add a leaf behaviour node with blackboard context.
    /// The factory function receives the current blackboard from the builder's scope.
    /// </summary>
    /// <param name="factory">Factory function that creates the behaviour, receiving the current blackboard.</param>
    /// <returns>This builder for method chaining.</returns>
    public TBuilder LeafWithBlackboard(Func<Blackboard?, Behaviour> factory)
    {
        var currentBB = GetCurrentBlackboard();
        var builder = new LeafBuilder(() => factory(currentBB));

        var parent = GetCurrentNodeBuilder();
        if (parent is null)
            throw new InvalidOperationException("Cannot add leaf node at root level. Use a composite or decorator as the root.");

        if (parent is CompositeBuilder compositeParent)
            compositeParent.AddChild(builder);
        else if (parent is DecoratorBuilder decoratorParent)
            decoratorParent.SetChild(builder);
        else
            throw new InvalidOperationException($"Cannot add leaf child to {parent.GetType().Name}");

        return Self;
    }

    /// <summary>
    /// End the current scope (composite, decorator, or blackboard) and pop it from the stack.
    /// </summary>
    /// <returns>This builder for method chaining.</returns>
    public TBuilder End()
    {
        if (_frameStack.Count == 0)
            throw new InvalidOperationException("No scope to end.");

        var completed = _frameStack.Pop();

        // If we just popped a blackboard frame, we're done
        if (completed is BlackboardFrame)
            return Self;

        // If we popped a composite or decorator, check if this is the root
        if (completed is CompositeFrame or DecoratorFrame)
        {
            // Check if there are any more node frames (composite/decorator) in the stack
            bool hasMoreNodeFrames = _frameStack.Any(f => f is CompositeFrame or DecoratorFrame);

            if (!hasMoreNodeFrames)
            {
                // No more node frames, this is the root
                _root = completed switch
                {
                    CompositeFrame cf => cf.Builder,
                    DecoratorFrame df => df.Builder,
                    _ => throw new InvalidOperationException("Unexpected frame type.")
                };
            }
        }

        return Self;
    }

    /// <summary>
    /// Build and return the final behaviour tree.
    /// </summary>
    /// <returns>The root behaviour of the constructed tree.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the tree is empty or incomplete.</exception>
    public Behaviour Build()
    {
        if (_root is null)
        {
            if (_frameStack.Count > 0)
                throw new InvalidOperationException("Tree is incomplete. Did you forget to call End()?");
            throw new InvalidOperationException("Tree is empty. Add at least one node.");
        }

        return _root.Build();
    }

    /// <summary>
    /// Preview the current state of the tree being built without consuming the builder.
    /// <para>
    /// Closes all open scopes, inserts a <see cref="Behaviours.Placeholder"/>
    /// at the current insertion point if needed, builds the tree, renders it,
    /// and then restores the builder to its previous state so construction can continue.
    /// </para>
    /// </summary>
    /// <returns>The built behaviour tree representing the current construction state.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the tree is empty (no nodes added).</exception>
    public Behaviour Preview()
    {
        if (_frameStack.Count == 0 && _root is null)
            throw new InvalidOperationException("Tree is empty. Add at least one node.");

        // Save current state
        var savedStack = new Stack<Frame>(_frameStack.Reverse());
        var savedRoot = _root;

        // Always add a placeholder at the current insertion point
        bool placeholderAdded = false;
        DecoratorBuilder? placeholderDecoratorParent = null;
        CompositeBuilder? placeholderCompositeParent = null;

        if (_frameStack.Count > 0)
        {
            // Walk the stack from top to find where to insert the placeholder:
            // - CompositeFrame → add as next child
            // - DecoratorFrame without child → add as child
            // - DecoratorFrame with child → skip, look at parent (next node will be a sibling)
            // - BlackboardFrame → skip, not a node frame
            foreach (var frame in _frameStack)
            {
                if (frame is CompositeFrame cf)
                {
                    var placeholder = new LeafBuilder(() => new Placeholder());
                    cf.Builder.AddChild(placeholder);
                    placeholderAdded = true;
                    placeholderCompositeParent = cf.Builder;
                    break;
                }
                if (frame is DecoratorFrame df)
                {
                    if (!df.Builder.HasChild)
                    {
                        var placeholder = new LeafBuilder(() => new Placeholder());
                        df.Builder.SetChild(placeholder);
                        placeholderAdded = true;
                        placeholderDecoratorParent = df.Builder;
                        break;
                    }
                    // Decorator already has a child — keep walking up the stack
                }
            }
        }

        // Close all open scopes
        while (_frameStack.Count > 0)
            End();

        // Build the tree
        var tree = Build();

        // Restore state
        _frameStack.Clear();
        foreach (var frame in savedStack.Reverse())
            _frameStack.Push(frame);
        _root = savedRoot;

        // Remove placeholder from builder tree
        if (placeholderAdded)
        {
            if (placeholderDecoratorParent is not null)
                placeholderDecoratorParent.ClearChild();
            else if (placeholderCompositeParent is not null)
                placeholderCompositeParent.RemoveLastChild();
        }

        return tree;
    }
}
