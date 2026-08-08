using CsTrees;
using CsTrees.Blackboard;
using Xunit;
using BB = CsTrees.Blackboard.Blackboard;

namespace CsTrees.Tests.Blackboard;

/// <summary>
/// 测试 [BlackboardKey] Source Generator 生成的端口系统。
/// </summary>
public class PortTests
{
    [Fact]
    public void SetupPorts_DefaultKeys_RegistersAccessWithDefaultKeyNames()
    {
        var bb = new BB();
        var btn = new PortTestDetectButton("DetectButton", bb);

        var accesses = bb.BehaviourKeyAccesses;
        Assert.Equal(2, accesses.Count);
        Assert.Contains(accesses, a => a.Key == "btn_x" && a.Access == Access.Write);
        Assert.Contains(accesses, a => a.Key == "btn_y" && a.Access == Access.Write);
    }

    [Fact]
    public void SetupPorts_CustomKeys_RegistersWithCustomKeyNames()
    {
        var bb = new BB();
        var btn = new PortTestDetectButton("DetectButton", bb, xKey: "custom_x", yKey: "custom_y");

        var accesses = bb.BehaviourKeyAccesses;
        Assert.Equal(2, accesses.Count);
        Assert.Contains(accesses, a => a.Key == "custom_x");
        Assert.Contains(accesses, a => a.Key == "custom_y");
    }

    [Fact]
    public void GetPortDeclarations_ReturnsAllPortInfo()
    {
        var decls = PortTestDetectButton.GetPortDeclarations();

        Assert.Equal(2, decls.Count);
        Assert.True(decls.ContainsKey("X"));
        Assert.True(decls.ContainsKey("Y"));

        var xDecl = decls["X"];
        Assert.Equal("btn_x", xDecl.DefaultKey);
        Assert.Equal(typeof(int), xDecl.ValueType);
        Assert.Equal(Access.Write, xDecl.Access);
    }

    [Fact]
    public void GetPortDeclarations_ReadWrite_ReturnsAllAccessLevels()
    {
        var decls = PortTestMoveTo.GetPortDeclarations();

        Assert.Equal(3, decls.Count);

        var txDecl = decls["TargetX"];
        Assert.Equal("target_x", txDecl.DefaultKey);
        Assert.Equal(typeof(float), txDecl.ValueType);
        Assert.Equal(Access.Read, txDecl.Access);

        var arrDecl = decls["Arrived"];
        Assert.Equal("arrived", arrDecl.DefaultKey);
        Assert.Equal(typeof(bool), arrDecl.ValueType);
        Assert.Equal(Access.Write, arrDecl.Access);
    }
}
