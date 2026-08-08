using System.Text.Json;
using CsTrees.Blackboard;
using CsTrees.Display;
using Xunit;

namespace CsTrees.Tests.Display;

public sealed class AsciiBlackboardRendererTests
{
    /// <summary>
    /// Test behaviour for blackboard tests.
    /// </summary>
    private sealed class TestBehaviour : Behaviour
    {
        public TestBehaviour(string name = "Test") : base(name) { }
        protected async override Task<Status> Update() => await Task.FromResult(Status.Success);
    }

    [Fact]
    public void AsciiBlackboard_RenderRegisteredKeys()
    {
        // Arrange
        var bb = new CsTrees.Blackboard.Blackboard();
        var client = new TestBehaviour();

        var foo = bb.GrantWrite<string>(client, "foo");
        var bar = bb.GrantWrite<int>(client, "bar");
        var unset = bb.GrantRead<string>(client, "unset");

        foo.Set("hello");
        bar.Set(42);
        // unset is not set

        // Act
        var items = bb.GetItems().OrderBy(i => i.Key);
        var renderer = new AsciiBlackboardRenderer();
        renderer.Begin();
        foreach (var item in items)
            renderer.WriteItem(item);
        renderer.End();
        var result = renderer.GetResult();

        // Assert
        var expected = string.Join(Environment.NewLine,
            "Blackboard Data",
            "    bar: 42",
            "    foo: hello",
            "    unset: -"
        ) + Environment.NewLine;

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AsciiBlackboard_FilteredItems()
    {
        // Arrange
        var bb = new CsTrees.Blackboard.Blackboard();
        var client = new TestBehaviour();

        bb.GrantWrite<string>(client, "foo").Set("a");
        bb.GrantWrite<string>(client, "bar").Set("b");
        bb.GrantWrite<string>(client, "baz").Set("c");

        // Act - filter to only keys starting with "ba"
        var items = bb.GetItems()
            .Where(i => i.Key.StartsWith("ba"))
            .OrderBy(i => i.Key);

        var result = CsTrees.Display.Display.AsciiBlackboard(items);

        // Assert
        var expected = string.Join(Environment.NewLine,
            "Blackboard Data",
            "    bar: b",
            "    baz: c"
        ) + Environment.NewLine;

        Assert.Equal(expected, result);
    }

    [Fact]
    public void AsciiBlackboard_JsonSerialization()
    {
        // Arrange
        var bb = new CsTrees.Blackboard.Blackboard();
        var client = new TestBehaviour();

        var complexObj = new ComplexData { Name = "test", Value = 42 };
        bb.GrantWrite<ComplexData>(client, "data").Set(complexObj);

        // Use JSON serialization for complex objects
        var jsonSymbols = new JsonSymbols();
        var items = bb.GetItems();

        // Act
        var result = CsTrees.Display.Display.AsciiBlackboard(items, jsonSymbols);

        // Assert
        Assert.Contains("data: ", result);
        Assert.Contains("\"Name\":\"test\"", result);
        Assert.Contains("\"Value\":42", result);
    }

    private sealed class JsonSymbols : BlackboardSymbols
    {
        private readonly JsonSerializerOptions _options = new() { WriteIndented = false };

        public override string FormatObject(object? value)
        {
            if (value is null)
                return "null";
            return JsonSerializer.Serialize(value, _options);
        }
    }

    private sealed class ComplexData
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }
}