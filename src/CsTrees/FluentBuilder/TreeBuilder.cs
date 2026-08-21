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

    // 追加式操作日志：记录每一次改动（含 leaf、end），为 Checkpoint/ResetTo 提供可逆历史。
    private abstract class Op { }

    /// <summary>
    /// PushComposite / PushDecorator 产生的记录。父节点通过 <see cref="NodeBuilder.Parent"/> 获取。
    /// </summary>
    private sealed class NodePushOp : Op
    {
        public NodeBuilder Builder { get; set; } = null!;
    }

    /// <summary>
    /// WithBlackboard 产生的记录。
    /// </summary>
    private sealed class BlackboardPushOp : Op
    {
        public Blackboard Blackboard { get; set; } = null!;
    }

    /// <summary>
    /// Leaf / LeafWithBlackboard 产生的记录。父节点通过 <see cref="NodeBuilder.Parent"/> 获取。
    /// </summary>
    private sealed class LeafOp : Op
    {
        public NodeBuilder Leaf { get; set; } = null!;
    }

    /// <summary>
    /// End 产生的记录。ClosedFrame 为被弹出的帧，SetRoot 标记该次 End 是否设置了根节点。
    /// </summary>
    private sealed class EndOp : Op
    {
        public Frame ClosedFrame { get; set; } = null!;
        public bool SetRoot { get; set; }
    }

    private readonly Stack<Frame> _frameStack = new();
    private readonly List<Op> _operations = new();
    private NodeBuilder? _root;

    /// <summary>
    /// Protected constructor to allow subclassing for domain-specific builders.
    /// </summary>
    protected TreeBuilder() { }

    /// <summary>
    /// 当前帧栈高度，供外部（如 BuildToolsBase）判断未关闭的作用域数量。
    /// </summary>
    public int FrameCount => _frameStack.Count;

    /// <summary>
    /// Get the current blackboard from the nearest blackboard frame in the stack.
    /// </summary>
    public Blackboard? GetCurrentBlackboard()
    {
        foreach (var frame in _frameStack)
        {
            if (frame is BlackboardFrame bbFrame)
                return bbFrame.Blackboard;
        }
        return null;
    }

    /// <summary>
    /// Get all distinct blackboards referenced by the current build operations.
    /// Includes blackboards from both open and closed scopes.
    /// </summary>
    public IEnumerable<Blackboard> GetAllBlackboards()
    {
        var seen = new HashSet<Blackboard>();
        foreach (var op in _operations)
        {
            if (op is BlackboardPushOp bbOp && seen.Add(bbOp.Blackboard))
                yield return bbOp.Blackboard;
        }
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
        _operations.Add(new NodePushOp { Builder = builder });
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
        _operations.Add(new NodePushOp { Builder = builder });
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
        _operations.Add(new BlackboardPushOp { Blackboard = blackboard });
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

        _operations.Add(new LeafOp { Leaf = builder });
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

        _operations.Add(new LeafOp { Leaf = builder });
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

        bool setRoot = false;

        // If we popped a composite or decorator, check if this is the root
        if (completed is CompositeFrame or DecoratorFrame)
        {
            // Check if there are any more node frames (composite/decorator) in the stack
            bool hasMoreNodeFrames = _frameStack.Any(f => f is CompositeFrame or DecoratorFrame);

            if (!hasMoreNodeFrames)
            {
                _root = completed switch
                {
                    CompositeFrame cf => cf.Builder,
                    DecoratorFrame df => df.Builder,
                    _ => throw new InvalidOperationException("Unexpected frame type.")
                };
                setRoot = true;
            }
        }

        _operations.Add(new EndOp { ClosedFrame = completed, SetRoot = setRoot });
        return Self;
    }

    /// <summary>
    /// 记录当前构建进度，返回检查点 id（等于操作日志的下标）。
    /// </summary>
    /// <returns>可用于 <see cref="ResetTo(int)"/> 的检查点 id。</returns>
    public int Checkpoint() => _operations.Count;

    /// <summary>
    /// 回滚到指定检查点：逆序撤销该检查点之后的所有操作（push、leaf、blackboard、end），
    /// 使 builder 精确恢复到检查点时刻的状态，之后可继续构建（分叉）。
    /// </summary>
    /// <param name="checkpoint">由 <see cref="Checkpoint"/> 返回的检查点 id。</param>
    /// <returns>This builder for method chaining.</returns>
    public TBuilder ResetTo(int checkpoint)
    {
        if (checkpoint < 0 || checkpoint > _operations.Count)
            throw new ArgumentOutOfRangeException(nameof(checkpoint));

        // 逆序回放：从当前末尾撤销到 checkpoint
        for (int i = _operations.Count - 1; i >= checkpoint; i--)
        {
            switch (_operations[i])
            {
                case NodePushOp push:
                    // 逆序回放时它压入的帧必在栈顶：先弹帧，再从父节点移除
                    _frameStack.Pop();
                    RemoveFromParent(push.Builder.Parent);
                    break;

                case LeafOp leaf:
                    RemoveFromParent(leaf.Leaf.Parent);
                    break;

                case BlackboardPushOp:
                    _frameStack.Pop();
                    break;

                case EndOp end:
                    if (end.SetRoot)
                        _root = null;
                    _frameStack.Push(end.ClosedFrame);
                    break;
            }
        }

        _operations.RemoveRange(checkpoint, _operations.Count - checkpoint);
        return Self;
    }

    /// <summary>
    /// 从父节点移除"刚添加"的节点：composite 移除末尾子节点，decorator 清空子节点。
    /// </summary>
    private static void RemoveFromParent(NodeBuilder? parent)
    {
        if (parent is CompositeBuilder composite)
            composite.RemoveLastChild();
        else if (parent is DecoratorBuilder decorator)
            decorator.ClearChild();
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
    /// only when the current decorator has no child yet (which would cause Build to fail),
    /// builds the tree, and then restores the builder to its previous state
    /// so construction can continue.
    /// </para>
    /// </summary>
    /// <returns>The built behaviour tree representing the current construction state.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the tree is empty (no nodes added).</exception>
    public Behaviour Preview()
    {
        if (_frameStack.Count == 0 && _root is null)
            throw new InvalidOperationException("Tree is empty. Add at least one node.");

        int checkpoint = Checkpoint();
        try
        {
            // 仅当 decorator 尚没有子节点时插入占位叶子（否则 Build 会失败），
            // composite 可以没有子节点，不需要 Placeholder。
            foreach (var frame in _frameStack)
            {
                if (frame is CompositeFrame)
                    break;
                if (frame is DecoratorFrame { Builder.HasChild: false })
                {
                    Leaf(() => new Placeholder());
                    break;
                }
            }

            // 正常收尾
            while (_frameStack.Count > 0)
                End();

            return Build();
        }
        finally
        {
            // 一步回滚到检查点：撤销占位与临时 End，恢复全部状态
            ResetTo(checkpoint);
            // 清空黑板：Preview 不留授权/值痕迹
            foreach (var bb in GetAllBlackboards())
                bb.Clear();
        }
    }
}
