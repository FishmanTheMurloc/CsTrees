using CsTrees.Blackboard;
using CsTrees.Composites;
using CsTrees.FluentBuilder;
using CsTrees.Tests.Blackboard;
using System.Threading.Tasks;
using Xunit;

namespace CsTrees.Tests.FluentBuilder;

public class BuilderExtensionsTests
{
    [Fact]
    public void GeneratedExtensionMethod_ShouldAddNodeToBuilder()
    {
        // Arrange
        var bb = new CsTrees.Blackboard.Blackboard();

        // Act - 使用 SG 生成的扩展方法
        var tree = new DefaultTreeBuilder()
            .WithBlackboard(bb)
                .Sequence("Main", false)
                    .PortTestSimpleOutput("Output1")
                    .PortTestSimpleOutput("Output2", labelKey: "custom_label")
                .End()
            .End()
            .Build();

        // Assert
        Assert.NotNull(tree);
        Assert.IsType<Sequence>(tree);
        var seq = (Sequence)tree;
        Assert.Equal(2, seq.Children.Count);
    }

    [Fact]
    public async Task GeneratedExtensionMethod_WithCustomKey_ShouldUseCustomKey()
    {
        // Arrange
        var bb = new CsTrees.Blackboard.Blackboard();

        // Act
        var tree = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .WithBlackboard(bb)
                    .PortTestSimpleOutput("Output", labelKey: "my_custom_key")
                .End()
            .End()
            .Build();

        // Assert
        Assert.NotNull(tree);
        // Tick the tree to verify the blackboard key works
        var status = (await tree.Tick().FirstAsync()).Status;
        // 验证行为节点能够正常运行
        Assert.Equal(Status.Success, status);
    }

    [Fact]
    public void GeneratedExtensionMethod_WithoutBlackboard_ShouldThrow()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new DefaultTreeBuilder()
                .Sequence("Main", false)
                    .PortTestSimpleOutput("Output")
                .End()
                .Build());

        Assert.Contains("Blackboard is required", ex.Message);
    }

    [Fact]
    public async Task GeneratedExtensionMethod_WithMultipleBlackboards_ShouldUseCorrectScope()
    {
        // Arrange
        var bb1 = new CsTrees.Blackboard.Blackboard();
        var bb2 = new CsTrees.Blackboard.Blackboard();

        // Act
        var tree = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .WithBlackboard(bb1)
                    .PortTestSimpleOutput("Output1")
                .End()
                .WithBlackboard(bb2)
                    .PortTestSimpleOutput("Output2")
                .End()
            .End()
            .Build();

        // Assert
        Assert.NotNull(tree);
        Assert.IsType<Sequence>(tree);
        var seq = (Sequence)tree;
        Assert.Equal(2, seq.Children.Count);

        // Tick the tree to verify both blackboards work
        var status = (await tree.Tick().FirstAsync()).Status;
        Assert.Equal(Status.Success, status);
    }

    [Fact]
    public async Task GeneratedExtensionMethod_WithOptionalParam_OmittingOptional_ShouldCompileAndRun()
    {
        // Arrange — 构造函数为 (string name, int multiplier, string? tag = null)
        // 验证可选参数默认值被正确处理，且 Blackboard 必选参数插入位置合法
        var bb = new CsTrees.Blackboard.Blackboard();

        // Act — 不传可选参数 tag，依赖默认值 null
        var tree = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .WithBlackboard(bb)
                    .PortTestOptionalParam("Node", 5)
                .End()
            .End()
            .Build();

        // Assert
        var status = (await tree.Tick().FirstAsync()).Status;
        Assert.Equal(Status.Success, status);
        // 验证必选参数 multiplier 被正确传递（5 * 2 = 10）
        var item = Assert.Single(bb.GetItems(), i => i.Key == "result");
        Assert.Equal(10, (int)item.Value!);
    }

    [Fact]
    public async Task GeneratedExtensionMethod_WithOptionalParam_ProvidingOptional_ShouldRespectValue()
    {
        // Arrange
        var bb = new CsTrees.Blackboard.Blackboard();

        // Act — 显式传入可选参数 tag 和 port key
        var tree = new DefaultTreeBuilder()
            .Sequence("Root", false)
                .WithBlackboard(bb)
                    .PortTestOptionalParam("Node", 3, tag: "myTag", resultKey: "custom_result")
                .End()
            .End()
            .Build();

        // Assert
        var status = (await tree.Tick().FirstAsync()).Status;
        Assert.Equal(Status.Success, status);
        // 验证 multiplier=3 被正确传递，且使用了自定义 port key
        var item = Assert.Single(bb.GetItems(), i => i.Key == "custom_result");
        Assert.Equal(6, (int)item.Value!);
    }
}