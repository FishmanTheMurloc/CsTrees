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
    public void Preview_IncompleteCompositeWithExistingChildren_NoPlaceholderInserted()
    {
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Success("Action1");

        // Preview should work without closing the scope
        var previewTree = builder.Preview();

        var sequence = Assert.IsType<Sequence>(previewTree);
        Assert.Equal("Root", sequence.Name);
        // Composite already has children, no Placeholder inserted
        Assert.Single(sequence.Children);
        Assert.IsType<Success>(sequence.Children[0]);

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
    public void Preview_NestedIncompleteComposites_NoPlaceholderInserted()
    {
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Sequence("Child", false);

        var previewTree = builder.Preview();

        var root = Assert.IsType<Sequence>(previewTree);
        var child = Assert.IsType<Sequence>(root.Children[0]);
        // Composites can be empty, no Placeholder inserted
        Assert.Empty(child.Children);
        Assert.Single(root.Children);
    }

    [Fact]
    public void Preview_ProducesRenderableAsciiTree()
    {
        var builder = new DefaultTreeBuilder()
            .Sequence("MakePizza", false)
                .Inverter("Inv");

        var previewTree = builder.Preview();
        var ascii = CsTrees.Display.Display.AsciiTree(previewTree, showStatus: false);

        Assert.Contains("MakePizza", ascii);
        Assert.Contains("Inv", ascii);
        Assert.Contains("...", ascii); // Placeholder inserted for decorator without child
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
    public void Preview_DecoratorWithChild_NoPlaceholderAdded()
    {
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Inverter("Inv")
                    .Success("Action");
        // Inv already has a child, Root already has children, no Placeholder needed

        var previewTree = builder.Preview();

        var root = Assert.IsType<Sequence>(previewTree);
        // Only Inv, no Placeholder (composite already has children)
        Assert.Single(root.Children);
        Assert.IsType<Inverter>(root.Children[0]);

        // Builder is still usable
        builder.End();
        builder.End();
        var builtTree = builder.Build();
        var builtRoot = Assert.IsType<Sequence>(builtTree);
        Assert.Single(builtRoot.Children);
    }

    [Fact]
    public void Preview_BlackboardFrameOnTop_NoPlaceholderWhenCompositeHasChildren()
    {
        var bb = new BB();
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false )
                .WithBlackboard(bb)
                    .Success("Action1");

        var previewTree = builder.Preview();

        var root = Assert.IsType<Sequence>(previewTree);
        // Composite already has children, no Placeholder inserted
        Assert.Single(root.Children);
        Assert.IsType<Success>(root.Children[0]);
    }

    // ========================================================================
    // Checkpoint + ResetTo tests
    // ========================================================================

    [Fact]
    public void Checkpoint_ResetTo_InvalidCheckpoint_ThrowsArgumentOutOfRangeException()
    {
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Success("Action1")
            .End();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.ResetTo(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.ResetTo(999));
    }

    [Fact]
    public void Checkpoint_ResetTo_CurrentPosition_NoOp()
    {
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Success("Action1")
            .End();

        var cp = builder.Checkpoint();
        builder.ResetTo(cp);

        var tree = builder.Build();
        var root = Assert.IsType<Sequence>(tree);
        Assert.Single(root.Children);
    }

    [Fact]
    public void Checkpoint_ResetTo_ComplexFork_MultipleBranchesFromSameCheckpoint()
    {
        var bb = new BB();

        // ---- Build common prefix (fully closed) ----
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .WithBlackboard(bb)
                    .Selector("Choice", false)
                        .Success("AlwaysFirst")
                    .End()
                .End();

        var cp = builder.Checkpoint();

        // ---- Branch A: add nested composite → decorator → leaf, then build ----
        var treeA = builder
            .Sequence("BranchA", false)
                .Inverter("InvertA")
                    .Failure("FlipA")
                .End()
            .End()
            .End()
            .Build();

        var rootA = Assert.IsType<Sequence>(treeA);
        Assert.Equal(2, rootA.Children.Count);
        var seqA = Assert.IsType<Sequence>(rootA.Children[1]);
        Assert.Equal("BranchA", seqA.Name);
        Assert.Single(seqA.Children);
        Assert.IsType<Inverter>(seqA.Children[0]);

        // ---- Reset to cp, add Branch B ----
        builder.ResetTo(cp);

        var treeB = builder
            .Parallel("BranchB", new ParallelPolicy.SuccessOnOne())
                .Success("Option1")
                .Failure("Option2")
            .End()
            .End()
            .Build();

        var rootB = Assert.IsType<Sequence>(treeB);
        Assert.Equal(2, rootB.Children.Count);
        var parallelB = Assert.IsType<Parallel>(rootB.Children[1]);
        Assert.Equal("BranchB", parallelB.Name);
        Assert.Equal(2, parallelB.Children.Count);

        // ---- Reset to cp again, add Branch C ----
        builder.ResetTo(cp);

        var treeC = builder
            .Selector("BranchC", false)
                .Leaf(() => new CsTrees.Behaviours.Running("Pending"))
            .End()
            .End()
            .Build();

        var rootC = Assert.IsType<Sequence>(treeC);
        Assert.Equal(2, rootC.Children.Count);
        var selectorC = Assert.IsType<Selector>(rootC.Children[1]);
        Assert.Equal("BranchC", selectorC.Name);
        Assert.Single(selectorC.Children);
        Assert.IsType<Running>(selectorC.Children[0]);
    }

    [Fact]
    public void Checkpoint_ResetTo_NestedUnfinishedScopes_PreservesFrameStack()
    {
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .Success("Before");

        var cp = builder.Checkpoint();

        // Branch 1: nested decorator + leaf
        builder
            .Inverter("Inv")
                .Success("InsideInv")
            .End();

        var tree1 = builder.End().Build();
        var root1 = Assert.IsType<Sequence>(tree1);
        Assert.Equal(2, root1.Children.Count);
        Assert.IsType<Inverter>(root1.Children[1]);

        // Reset to cp, add different nested composite
        builder.ResetTo(cp);

        builder
            .Selector("AltChoice", false)
                .Success("Alt1")
                .Success("Alt2")
            .End();

        var tree2 = builder.End().Build();
        var root2 = Assert.IsType<Sequence>(tree2);
        Assert.Equal(2, root2.Children.Count);
        var alt = Assert.IsType<Selector>(root2.Children[1]);
        Assert.Equal("AltChoice", alt.Name);
        Assert.Equal(2, alt.Children.Count);
    }

    [Fact]
    public void Checkpoint_ResetTo_ThroughBlackboardScope_CorrectlyRestores()
    {
        var bb1 = new BB();
        var bb2 = new BB();

        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .WithBlackboard(bb1)
                    .Success("InBb1")
                .End();

        var cp = builder.Checkpoint();

        // Branch A: push another blackboard scope + leaf
        builder
            .WithBlackboard(bb2)
                .Success("InBb2")
            .End()
            .End();

        var treeA = builder.Build();
        var rootA = Assert.IsType<Sequence>(treeA);
        Assert.Equal(2, rootA.Children.Count);

        // Reset — blackboard scope should be gone
        builder.ResetTo(cp);

        // Push a different blackboard + nested composite
        var bb3 = new BB();
        builder
            .WithBlackboard(bb3)
                .Selector("S3", false)
                    .Success("InBb3")
                .End()
            .End()
            .End();

        var treeB = builder.Build();
        var rootB = Assert.IsType<Sequence>(treeB);
        Assert.Equal(2, rootB.Children.Count);
        var s3 = Assert.IsType<Selector>(rootB.Children[1]);
        Assert.Equal("S3", s3.Name);
    }

    [Fact]
    public void Checkpoint_ResetTo_FullResetToZero_ErasesEverything()
    {
        var builder = new DefaultTreeBuilder();
        var cp = builder.Checkpoint(); // cp == 0

        builder
            .Sequence("Root", false)
                .Success("Action1")
            .End();

        // Reset to 0 BEFORE building — everything should be erased
        builder.ResetTo(cp);

        // Frame stack and root should be empty
        Assert.Throws<InvalidOperationException>(() => builder.Build());

        // Build a fresh tree from scratch
        var newTree = builder
            .Selector("Fresh", false)
                .Failure("NewAction")
            .End()
            .Build();

        Assert.IsType<Selector>(newTree);
        Assert.Equal("Fresh", newTree.Name);
    }

    [Fact]
    public void Checkpoint_ResetTo_MultipleCheckpoints_ChainedResets()
    {
        var builder = new DefaultTreeBuilder()
            .Sequence("Root", false);

        var cp0 = builder.Checkpoint();

        builder.Success("First");
        var cp1 = builder.Checkpoint();

        builder.Success("Second");
        var cp2 = builder.Checkpoint();

        builder.Success("Third");
        var cp3 = builder.Checkpoint();

        // Reset to cp1 → only "First" remains
        builder.ResetTo(cp1);
        builder.End();
        var t1 = builder.Build();
        Assert.Equal(1, ((Sequence)t1).Children.Count);

        // Reset to cp0 → no children
        builder.ResetTo(cp0);
        builder.End();
        var t0 = builder.Build();
        Assert.Equal(0, ((Sequence)t0).Children.Count);

        // cp3 > current op count after reset — should fail
        Assert.Throws<ArgumentOutOfRangeException>(() => builder.ResetTo(cp3));

        // Rebuild to cp2-equivalent state
        builder.ResetTo(cp0);
        builder.Success("First");
        builder.Success("Second");
        var t2 = builder.End().Build();
        Assert.Equal(2, ((Sequence)t2).Children.Count);
    }

    [Fact]
    public void Checkpoint_ResetTo_DiamondPattern_TwoNestedBranchesThenMerge()
    {
        // Simulates: build shared prefix → checkpoint → two fork paths → both build valid trees
        var builder = new DefaultTreeBuilder()
            .Sequence("Mission", false)
                .Success("Initialize")
            ;

        var forkPoint = builder.Checkpoint();

        // Fork 1: attack path (Sequence of actions under a Retry decorator)
        var attackTree = builder
            .Retry("RetryAttack", 3)
                .Sequence("AttackChain", false)
                    .Success("Aim")
                    .Success("Fire")
                .End()
            .End()
            .End()
            .Build();

        var mission = Assert.IsType<Sequence>(attackTree);
        Assert.Equal(2, mission.Children.Count);
        var retry = Assert.IsType<Retry>(mission.Children[1]);
        Assert.Equal("RetryAttack", retry.Name);
        var attackChain = Assert.IsType<Sequence>(retry.Children[0]);
        Assert.Equal("AttackChain", attackChain.Name);
        Assert.Equal(2, attackChain.Children.Count);

        // Reset to fork point
        builder.ResetTo(forkPoint);

        // Fork 2: retreat path (Selector with multiple escape options)
        var retreatTree = builder
            .Selector("RetreatOptions", false)
                .Inverter("CheckSafe")
                    .Failure("IsDangerous")
                .End()
                .Success("RunAway")
            .End()
            .End()
            .Build();

        mission = Assert.IsType<Sequence>(retreatTree);
        Assert.Equal(2, mission.Children.Count);
        var retreatOpts = Assert.IsType<Selector>(mission.Children[1]);
        Assert.Equal("RetreatOptions", retreatOpts.Name);
        Assert.Equal(2, retreatOpts.Children.Count);
        var checkSafe = Assert.IsType<Inverter>(retreatOpts.Children[0]);
        Assert.Equal("CheckSafe", checkSafe.Name);
    }
}