using CsTrees.Behaviours;
using CsTrees.Composites;
using System.Threading.Tasks;
using Xunit;

namespace CsTrees.Tests.Display;

public sealed class AsciiTreeRendererTests
{
    [Fact]
    public async Task AsciiTree_ShowStatus_SequenceWithRunningChild()
    {
        // Arrange: matches the py_trees documentation example
        //   Sequence [*]
        //       --> Action 1 [*] -- running
        //       --> Action 2 [-]
        //       --> Action 3 [-]
        var sequence = new Sequence("Sequence", memory: false,
        [
            new Running("Action 1"),
            new Success("Action 2"),
            new Success("Action 3"),
        ]);

        // Tick the tree so statuses are set
        await sequence.Tick().ToListAsync();

        // Act
        var result = CsTrees.Display.Display.AsciiTree(sequence, showStatus: true, showFeedbackMessage: true);

        // Assert
        var expected = string.Join(Environment.NewLine,
            "[-] Sequence [*]",
            "    --> Action 1 [*] -- running",
            "    --> Action 2 [-]",
            "    --> Action 3 [-]"
        ) + Environment.NewLine;

        Assert.Equal(expected, result);
    }
}
