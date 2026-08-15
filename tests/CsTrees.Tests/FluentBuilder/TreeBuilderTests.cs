using CsTrees.Behaviours;
using CsTrees.FluentBuilder;
using CsTrees.Composites;
using CsTrees.Decorators;
using Xunit;
using Display = CsTrees.Display.Display;
using Parallel = CsTrees.Composites.Parallel;

namespace CsTrees.Tests.FluentBuilder;

// Type alias to avoid conflict with namespace CsTrees.Tests.Blackboard
using BB = CsTrees.Blackboard.Blackboard;

public class TreeBuilderTests
{
    [Fact]
    public void Build_EmptyTree_ThrowsInvalidOperationException()
    {
        var builder = new DefaultTreeBuilder();
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_SingleLeafAtRoot_ThrowsInvalidOperationException()
    {
        var builder = new DefaultTreeBuilder();
        Assert.Throws<InvalidOperationException>(() => builder.Success("Test"));
    }

    [Fact]
    public void Build_SimpleSequence_ReturnsSequenceNode()
    {
        var tree = new DefaultTreeBuilder()
            .Sequence("Main", false)
                .Success("Action1")
                .Success("Action2")
            .End()
            .Build();

        Assert.IsType<Sequence>(tree);
        Assert.Equal("Main", tree.Name);
        Assert.Equal(2, tree.Children.Count);
    }

    [Fact]
    public void Build_SimpleSelector_ReturnsSelectorNode()
    {
        var tree = new DefaultTreeBuilder()
            .Selector("Main", false)
                .Success("Action1")
                .Failure("Action2")
            .End()
            .Build();

        Assert.IsType<Selector>(tree);
        Assert.Equal("Main", tree.Name);
        Assert.Equal(2, tree.Children.Count);
    }

    [Fact]
    public void Build_NestedComposites_ReturnsCorrectHierarchy()
    {
        var tree = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Selector("Choice", false)
                    .Success("Option1")
                    .Failure("Option2")
                .End()
                .Success("Final")
            .End()
            .Build();

        var rootSequence = Assert.IsType<Sequence>(tree);
        Assert.Equal("Root", rootSequence.Name);
        Assert.Equal(2, rootSequence.Children.Count);

        var selector = Assert.IsType<Selector>(rootSequence.Children[0]);
        Assert.Equal("Choice", selector.Name);
        Assert.Equal(2, selector.Children.Count);
    }

    [Fact]
    public void Build_DecoratorAsRoot_ReturnsDecoratorNode()
    {
        var tree = new DefaultTreeBuilder()
            .Inverter("InvertRoot")
                .Success("Action")
            .End()
            .Build();

        var inverter = Assert.IsType<Inverter>(tree);
        Assert.Equal("InvertRoot", inverter.Name);
        Assert.Single(inverter.Children);
    }

    [Fact]
    public void Build_DecoratorInsideComposite_ReturnsCorrectStructure()
    {
        var tree = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Inverter("Invert")
                    .Failure("Action")
                .End()
            .End()
            .Build();

        var sequence = Assert.IsType<Sequence>(tree);
        var inverter = Assert.IsType<Inverter>(sequence.Children[0]);
        Assert.Single(inverter.Children);
    }

    [Fact]
    public void Build_SequenceWithMemory_ReturnsSequenceWithMemoryEnabled()
    {
        var tree = new DefaultTreeBuilder()
            .Sequence("Main", true)
                .Success("Action1")
            .End()
            .Build();

        var sequence = Assert.IsType<Sequence>(tree);
        Assert.True(sequence.Memory);
    }

    [Fact]
    public void Build_SelectorWithMemory_ReturnsSelectorWithMemoryEnabled()
    {
        var tree = new DefaultTreeBuilder()
            .Selector("Main", true)
                .Success("Action1")
            .End()
            .Build();

        var selector = Assert.IsType<Selector>(tree);
        Assert.True(selector.Memory);
    }

    [Fact]
    public void Build_Parallel_ReturnsParallelNode()
    {
        var tree = new DefaultTreeBuilder()
            .Parallel("Main", new ParallelPolicy.SuccessOnAll())
                .Success("Action1")
                .Success("Action2")
            .End()
            .Build();

        var parallel = Assert.IsType<Parallel>(tree);
        Assert.Equal("Main", parallel.Name);
        Assert.Equal(2, parallel.Children.Count);
    }

    [Fact]
    public void Build_ParallelWithCustomPolicy_ReturnsParallelWithPolicy()
    {
        var tree = new DefaultTreeBuilder()
            .Parallel("Main", new ParallelPolicy.SuccessOnOne())
                .Success("Action1")
                .Failure("Action2")
            .End()
            .Build();

        var parallel = Assert.IsType<Parallel>(tree);
        Assert.IsType<ParallelPolicy.SuccessOnOne>(parallel.Policy);
    }

    [Fact]
    public void Build_MultipleDecorators_WrapsCorrectly()
    {
        var tree = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Inverter("Inv1")
                    .Inverter("Inv2")
                        .Failure("Action")
                    .End()
                .End()
            .End()
            .Build();

        var sequence = Assert.IsType<Sequence>(tree);
        var inv1 = Assert.IsType<Inverter>(sequence.Children[0]);
        var inv2 = Assert.IsType<Inverter>(inv1.Children[0]);
        Assert.Single(inv2.Children);
    }

    [Fact]
    public void Build_CustomLeafFactory_ReturnsCustomBehaviour()
    {
        var tree = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Leaf(() => new CsTrees.Behaviours.Success("CustomAction"))
            .End()
            .Build();

        var sequence = Assert.IsType<Sequence>(tree);
        Assert.Single(sequence.Children);
        Assert.Equal("CustomAction", sequence.Children[0].Name);
    }

    [Fact]
    public void End_NoCompositeToPop_ThrowsInvalidOperationException()
    {
        var builder = new DefaultTreeBuilder();
        Assert.Throws<InvalidOperationException>(() => builder.End());
    }

    [Fact]
    public void Build_IncompleteTree_ThrowsInvalidOperationException()
    {
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Success("Action");
        // Missing End()

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    // ========================================================================
    // WithBlackboard tests
    // ========================================================================

    [Fact]
    public void WithBlackboard_AtRoot_ThenSequence_BuildsSuccessfully()
    {
        var bb = new BB();
        
        var tree = new DefaultTreeBuilder()
            .WithBlackboard(bb)
                .Sequence("Root", false)
                    .Success("Action1")
                    .Success("Action2")
                .End()
            .End()
            .Build();

        Assert.IsType<Sequence>(tree);
    }

    [Fact]
    public void WithBlackboard_InsideComposite_BuildsSuccessfully()
    {
        var bb = new BB();
        
        var tree = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .WithBlackboard(bb)
                    .Success("Action1")
                .End()
                .Success("Action2")
            .End()
            .Build();

        var sequence = Assert.IsType<Sequence>(tree);
        Assert.Equal(2, sequence.Children.Count);
    }

    [Fact]
    public void WithBlackboard_MultipleScopes_BuildsSuccessfully()
    {
        var bb1 = new BB();
        var bb2 = new BB();
        
        var tree = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .WithBlackboard(bb1)
                    .Success("Action1")
                .End()
                .WithBlackboard(bb2)
                    .Success("Action2")
                .End()
            .End()
            .Build();

        var sequence = Assert.IsType<Sequence>(tree);
        Assert.Equal(2, sequence.Children.Count);
    }

    [Fact]
    public void WithBlackboard_NestedScopes_BuildsSuccessfully()
    {
        var bb1 = new BB();
        var bb2 = new BB();
        
        var tree = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .WithBlackboard(bb1)
                    .Sequence("Branch1", false)
                        .WithBlackboard(bb2)
                            .Success("Action1")
                        .End()
                        .Success("Action2")
                    .End()
                .End()
            .End()
            .Build();

        Assert.IsType<Sequence>(tree);
    }

    [Fact]
    public void WithBlackboard_NullBlackboard_ThrowsArgumentNullException()
    {
        var builder = new DefaultTreeBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.WithBlackboard(null!));
    }

    // ========================================================================
    // Preview tests
    // ========================================================================

    [Fact]
    public void Preview_EmptyTree_ThrowsInvalidOperationException()
    {
        var builder = new DefaultTreeBuilder();
        Assert.Throws<InvalidOperationException>(() => builder.Preview());
    }

    [Fact]
    public void Preview_CompleteTree_ReturnsSameAsBuild()
    {
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Success("Action1")
                .Success("Action2")
            .End();

        var previewTree = builder.Preview();
        var builtTree = builder.Build();

        Assert.IsType<Sequence>(previewTree);
        Assert.Equal("Root", previewTree.Name);
        Assert.Equal(2, previewTree.Children.Count);
        Assert.Equal("Root", builtTree.Name);
        Assert.Equal(2, builtTree.Children.Count);
    }

    [Fact]
    public void Preview_IncompleteComposite_InsertsPlaceholderAndDoesNotConsumeBuilder()
    {
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Success("Action1");

        // Preview should work without closing the scope
        var previewTree = builder.Preview();

        var sequence = Assert.IsType<Sequence>(previewTree);
        Assert.Equal("Root", sequence.Name);
        // Action1 + Placeholder = 2 children
        Assert.Equal(2, sequence.Children.Count);
        Assert.IsType<Placeholder>(sequence.Children[1]);

        // Builder should still be usable — can continue adding nodes
        builder.Success("Action2");
        builder.End();
        var builtTree = builder.Build();

        var builtSequence = Assert.IsType<Sequence>(builtTree);
        // Action1 + Action2 = 2 children (no placeholder)
        Assert.Equal(2, builtSequence.Children.Count);
        Assert.All(builtSequence.Children, c => Assert.IsNotType<Placeholder>(c));
    }

    [Fact]
    public void Preview_DecoratorWithoutChild_InsertsPlaceholderAndRestores()
    {
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Inverter("Inv");
        // Inv has no child yet

        var previewTree = builder.Preview();

        var sequence = Assert.IsType<Sequence>(previewTree);
        var inverter = Assert.IsType<Inverter>(sequence.Children[0]);
        // Placeholder was inserted as decorator's child
        Assert.Single(inverter.Children);
        Assert.IsType<Placeholder>(inverter.Children[0]);

        // Builder is still usable — add a real child
        builder.Failure("Action");
        builder.End();
        builder.End();
        var builtTree = builder.Build();

        var builtSequence = Assert.IsType<Sequence>(builtTree);
        var builtInverter = Assert.IsType<Inverter>(builtSequence.Children[0]);
        Assert.Single(builtInverter.Children);
        Assert.IsNotType<Placeholder>(builtInverter.Children[0]);
    }

    [Fact]
    public void Preview_NestedIncompleteComposites_InsertsSinglePlaceholderAtDeepest()
    {
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Sequence("Child", false);

        var previewTree = builder.Preview();

        var root = Assert.IsType<Sequence>(previewTree);
        var child = Assert.IsType<Sequence>(root.Children[0]);
        // Only one placeholder at the deepest composite
        Assert.Single(child.Children);
        Assert.IsType<Placeholder>(child.Children[0]);

        // Root also got a placeholder (for the next insertion point after "Child")
        // Actually, only the topmost frame gets a placeholder
        // Root has one child: "Child" composite + the placeholder is in "Child"
        Assert.Single(root.Children);
    }

    [Fact]
    public void Preview_ProducesRenderableAsciiTree()
    {
        var builder = new DefaultTreeBuilder()
            .Sequence("MakePizza", false)
                .Success("PrepareDough")
                .Parallel("ParallelStep", new ParallelPolicy.SuccessOnAll());

        var previewTree = builder.Preview();
        var ascii = CsTrees.Display.Display.AsciiTree(previewTree, showStatus: false);

        Assert.Contains("MakePizza", ascii);
        Assert.Contains("PrepareDough", ascii);
        Assert.Contains("ParallelStep", ascii);
        Assert.Contains("...", ascii); // Placeholder name
    }

    [Fact]
    public void Preview_MultipleCalls_ReturnConsistentResults()
    {
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Success("Action1");

        var preview1 = builder.Preview();
        var preview2 = builder.Preview();

        var seq1 = Assert.IsType<Sequence>(preview1);
        var seq2 = Assert.IsType<Sequence>(preview2);

        // Both previews should show same structure
        Assert.Equal(seq1.Children.Count, seq2.Children.Count);
    }

    [Fact]
    public void Preview_DecoratorWithChild_AddsPlaceholderToParentComposite()
    {
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Inverter("Inv")
                    .Success("Action");
        // Inv already has a child, but scope is still open

        var previewTree = builder.Preview();

        var root = Assert.IsType<Sequence>(previewTree);
        // Inv + Placeholder = 2 children in Root
        Assert.Equal(2, root.Children.Count);
        Assert.IsType<Inverter>(root.Children[0]);
        Assert.IsType<Placeholder>(root.Children[1]);

        // Builder is still usable
        builder.End();
        builder.End();
        var builtTree = builder.Build();
        var builtRoot = Assert.IsType<Sequence>(builtTree);
        // Only Inv, no placeholder
        Assert.Single(builtRoot.Children);
    }

    [Fact]
    public void Preview_BlackboardFrameOnTop_StillAddsPlaceholder()
    {
        var bb = new BB();
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false )
                .WithBlackboard(bb)
                    .Success("Action1");

        var previewTree = builder.Preview();

        var root = Assert.IsType<Sequence>(previewTree);
        // Action1 + Placeholder = 2 children
        Assert.Equal(2, root.Children.Count);
        Assert.IsType<Placeholder>(root.Children[1]);
    }
}