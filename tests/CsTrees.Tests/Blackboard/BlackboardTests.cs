using CsTrees;
using Xunit;
using ActivityType = CsTrees.Blackboard.ActivityType;

namespace CsTrees.Tests.Blackboard;

/// <summary>
/// Ported from py_trees.tests.test_blackboard
/// </summary>
public class BlackboardTests
{
    /// <summary>
    /// Test behaviour for blackboard tests (replaces py_trees.blackboard.Client).
    /// </summary>
    private sealed class TestBehaviour : Behaviour
    {
        public TestBehaviour(string name = "Client") : base(name) { }
        protected async override Task<Status> Update() => await Task.FromResult(Status.Success);
    }

    /// <summary>
    /// Test activity stream recording, ported from py_trees.tests.test_blackboard.test_activity_stream.
    /// </summary>
    [Fact]
    public void ActivityStream()
    {
        // Setup
        var bb = new CsTrees.Blackboard.Blackboard();
        bb.EnableActivityStream(100);

        var client = new TestBehaviour();

        // Register keys (py_trees: register_key for READ and WRITE)
        var foo = bb.GrantRead<string>(client, "foo");
        var dude = bb.GrantRead<string>(client, "dude");
        var spaghetti = bb.GrantWrite<object>(client, "spaghetti");
        var motley = bb.GrantWrite<object>(client, "motley");

        // Step 1: Read non-existent key -> NO_KEY
        try { _ = dude.Get(); } catch (Exception) { /* expected */ }

        // Skip step 2-3: ACCESS_DENIED for unregistered keys (cannot test in CsTrees)

        // Step 4: First write -> INITIALISED
        spaghetti.Set(new { type = "Carbonara", quantity = 1 });

        // Step 5: Second write -> WRITE
        spaghetti.Set(new { type = "Gnocchi", quantity = 2 });

        // Step 6: First write to motley -> INITIALISED
        motley.Set(new { nested = "nested" });

        // Skip step 7: Write nested property (not supported in CsTrees)

        // Step 8: Read complex object -> ACCESSED
        _ = motley.Get();

        // Step 9: Set with overwrite=false on existing key -> NO_OVERWRITE
        Assert.False(spaghetti.Set(new { type = "Bolognese", quantity = 3 }, overwrite: false));

        // Step 10: Unset key -> UNSET
        spaghetti.Unset();

        // Verify activity stream
        var activities = bb.ActivityStream!.Data;

        var expectedTypes = new[]
        {
            ActivityType.NoKey,        // Step 1
            ActivityType.Initialised,  // Step 4
            ActivityType.Write,        // Step 5
            ActivityType.Initialised,  // Step 6
            ActivityType.Accessed,     // Step 8
            ActivityType.NoOverwrite,  // Step 9
            ActivityType.Unset,        // Step 10
        };

        Assert.Equal(expectedTypes.Length, activities.Count);
        for (int i = 0; i < expectedTypes.Length; i++)
        {
            Assert.Equal(expectedTypes[i], activities[i].ActivityType);
        }

        // Verify specific keys
        Assert.Equal("dude", activities[0].Key);        // NO_KEY
        Assert.Equal("spaghetti", activities[1].Key);   // INITIALISED
        Assert.Equal("spaghetti", activities[2].Key);   // WRITE
        Assert.Equal("motley", activities[3].Key);      // INITIALISED
        Assert.Equal("motley", activities[4].Key);      // ACCESSED
        Assert.Equal("spaghetti", activities[5].Key);   // NO_OVERWRITE
        Assert.Equal("spaghetti", activities[6].Key);   // UNSET

        // Verify previous/current values for WRITE
        Assert.NotNull(activities[2].PreviousValue);
        Assert.NotNull(activities[2].CurrentValue);

        // Verify spaghetti was unset
        Assert.False(bb.GetItems().First(i => i.Key == "spaghetti").HasValue);
    }
}