using CsTrees.Blackboard;
using Xunit;

namespace CsTrees.Tests.Display;

public sealed class AsciiActivityStreamRendererTests
{
    /// <summary>
    /// Test behaviour for activity stream tests.
    /// </summary>
    private sealed class TestBehaviour : Behaviour
    {
        public TestBehaviour(string name = "Test") : base(name) { }
        protected async override Task<Status> Update() => await Task.FromResult(Status.Success);
    }

    /// <summary>
    /// Tests all activity types including NoKey, Accessed, NoOverwrite, and Unset.
    /// Ported from py_trees.tests.test_blackboard.test_activity_stream
    /// </summary>
    [Fact]
    public void AsciiActivityStream_AllActivityTypes()
    {
        // Arrange
        var bb = new CsTrees.Blackboard.Blackboard();
        bb.EnableActivityStream(100);

        var client = new TestBehaviour("Client");

        var dude = bb.GrantRead<string>(client, "dude");
        var spaghetti = bb.GrantWrite<object>(client, "spaghetti");
        var motley = bb.GrantWrite<object>(client, "motley");

        // NoKey — read non-existent key
        try { _ = dude.Get(); } catch (Exception) { /* expected */ }

        // Initialised — first write
        spaghetti.Set(new { type = "Carbonara", quantity = 1 });

        // Write — overwrite existing key
        spaghetti.Set(new { type = "Gnocchi", quantity = 2 });

        // Initialised — first write to motley
        motley.Set(new { nested = "nested" });

        // Accessed — read complex object
        _ = motley.Get();

        // NoOverwrite — set with overwrite=false on existing key
        spaghetti.Set(new { type = "Bolognese", quantity = 3 }, overwrite: false);

        // Unset — remove key
        spaghetti.Unset();

        // Act
        var result = CsTrees.Display.Display.AsciiActivityStream(bb.ActivityStream!.Data);

        // Assert
        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(8, lines.Length);  // title + 7 activity items

        Assert.Equal("Blackboard Activity Stream", lines[0]);
        Assert.Contains("NO_KEY", lines[1]);
        Assert.Contains("key does not yet exist", lines[1]);

        Assert.Contains("INITIALISED", lines[2]);
        Assert.Contains("->", lines[2]);

        Assert.Contains("WRITE", lines[3]);
        Assert.Contains("->", lines[3]);

        Assert.Contains("INITIALISED", lines[4]);
        Assert.Contains("->", lines[4]);

        Assert.Contains("ACCESSED", lines[5]);
        Assert.Contains("<->", lines[5]);

        Assert.Contains("NO_OVERWRITE", lines[6]);
        Assert.Contains("#", lines[6]);

        Assert.Contains("UNSET", lines[7]);
    }

    /// <summary>
    /// Tests rendering an empty activity stream.
    /// </summary>
    [Fact]
    public void AsciiActivityStream_Empty()
    {
        // Arrange
        var stream = new CsTrees.Blackboard.ActivityStream();

        // Act
        var result = CsTrees.Display.Display.AsciiActivityStream(stream.Data);

        // Assert — only title, no items
        Assert.Equal("Blackboard Activity Stream" + Environment.NewLine, result);
    }

    /// <summary>
    /// Tests custom symbols for the activity stream renderer.
    /// </summary>
    [Fact]
    public void AsciiActivityStream_CustomSymbols()
    {
        // Arrange
        var bb = new CsTrees.Blackboard.Blackboard();
        bb.EnableActivityStream(100);

        var writer = new TestBehaviour("Writer");
        var reader = new TestBehaviour("Reader");

        var foo = bb.GrantWrite<string>(writer, "foo");
        var fooRead = bb.GrantRead<string>(reader, "foo");

        foo.Set("bar");
        _ = fooRead.Get();

        var customSymbols = new CustomArrowSymbols();

        // Act
        var result = CsTrees.Display.Display.AsciiActivityStream(bb.ActivityStream!.Data, customSymbols);

        // Assert — custom arrows should appear
        Assert.Contains(">>", result);  // custom right arrow
        Assert.Contains("<<", result);  // custom left arrow
    }

    private sealed class CustomArrowSymbols : CsTrees.Display.ActivityStreamSymbols
    {
        public override string RightArrow => ">>";
        public override string LeftArrow => "<<";
    }
}
