using CsTrees;
using CsTrees.Blackboard;
using CsTrees.FluentBuilder;

namespace CsTrees.Tests.Blackboard;

/// <summary>
/// 带输出端口的测试行为。
/// </summary>
[System.ComponentModel.Description("检测按钮位置的行为")]
public partial class PortTestDetectButton : Behaviour
{
    [BlackboardKey("btn_x", Access = Access.Write)]
    [System.ComponentModel.Description("将写入检测到的X坐标到BB上")]
    public BehaviourKeyAccess<int> X { get; private set; } = null!;

    [BlackboardKey("btn_y", Access = Access.Write)]
    [System.ComponentModel.Description("将写入检测到的Y坐标到BB上")]
    public BehaviourKeyAccess<int> Y { get; private set; } = null!;

    private PortTestDetectButton(string name) : base(name) { }

    protected async override Task<Status> Update()
    {
        X.Set(42);
        Y.Set(99);
        return await Task.FromResult(Status.Success);
    }
}

/// <summary>
/// 带 Read/Write 混合端口的测试行为。
/// </summary>
public partial class PortTestMoveTo : Behaviour
{
    [BlackboardKey("target_x", Access = Access.Read)]
    [System.ComponentModel.Description("目标X坐标")]
    public BehaviourKeyAccess<float> TargetX { get; private set; } = null!;

    [BlackboardKey("target_y", Access = Access.Read)]
    [System.ComponentModel.Description("目标Y坐标")]
    public BehaviourKeyAccess<float> TargetY { get; private set; } = null!;

    [BlackboardKey("arrived", Access = Access.Write)]
    [System.ComponentModel.Description("是否已到达")]
    public BehaviourKeyAccess<bool> Arrived { get; private set; } = null!;

    public PortTestMoveTo(string name) : base(name) { }

    protected async override Task<Status> Update()
    {
        var tx = TargetX.Get();
        var ty = TargetY.Get();
        Arrived.Set(true);
        return await Task.FromResult(Status.Success);
    }
}

/// <summary>
/// 不指定 Key 时使用属性名作为默认键名。
/// </summary>
[GenerateTreeBuilderExtension]
public partial class PortTestSimpleOutput : Behaviour
{
    [BlackboardKey(Access = Access.Write)]
    public BehaviourKeyAccess<string> Label { get; private set; } = null!;

    private PortTestSimpleOutput(string name) : base(name) { }

    protected async override Task<Status> Update()
    {
        Label.Set("hello");
        return await Task.FromResult(Status.Success);
    }
}

/// <summary>
/// 带扩展构造函数的测试行为，用于验证 SG 为每个构造函数生成重载。
/// </summary>
public partial class PortTestExtended : Behaviour
{
    [BlackboardKey("speed", Access = Access.Write)]
    public BehaviourKeyAccess<float> Speed { get; private set; } = null!;

    private readonly int _id;

    private PortTestExtended(string name) : base(name)
    {
        _id = 0;
    }

    private PortTestExtended(string name, int id) : base(name)
    {
        _id = id;
    }

    protected async override Task<Status> Update()
    {
        Speed.Set(_id * 1.0f);
        return await Task.FromResult(Status.Success);
    }
}

/// <summary>
/// 带可选参数构造函数的测试行为，用于验证 SG 正确处理默认值参数
/// （可选参数必须位于必选参数之后，Blackboard 必选参数需插入到可选参数之前）。
/// </summary>
[GenerateTreeBuilderExtension]
public partial class PortTestOptionalParam : Behaviour
{
    [BlackboardKey("result", Access = Access.Write)]
    public BehaviourKeyAccess<int> Result { get; private set; } = null!;

    private readonly int _multiplier;
    private readonly string? _tag;

    private PortTestOptionalParam(string name, int multiplier, string? tag = null) : base(name)
    {
        _multiplier = multiplier;
        _tag = tag;
    }

    protected async override Task<Status> Update()
    {
        Result.Set(_multiplier * 2);
        return await Task.FromResult(Status.Success);
    }
}

/// <summary>
/// 辅助测试行为，用于向 Blackboard 写入数据。
/// </summary>
public sealed class PortTestWriter : Behaviour
{
    public PortTestWriter() : base("Writer") { }
    protected async override Task<Status> Update() => await Task.FromResult(Status.Success);
}
