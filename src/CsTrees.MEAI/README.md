# CsTrees.MEAI [![NuGet Version](https://img.shields.io/nuget/v/CsTrees.MEAI.svg?style=flat-square)](https://www.nuget.org/packages/CsTrees.MEAI)

将 [CsTrees](https://github.com/FishmanTheMurloc/CsTrees) 行为树框架暴露为工具调用接口的集成包，供 Microsoft.Extensions.AI (MEAI) 框架构建和运行行为树。

## 原理

我们已经为 Builder 写过一遍 Catalog 了，对于 LLM 来说，构建行为树使用的 Function Calling 只是形式不同，本质依然是在使用流式构建器。那么我们就可以按照 Catalog 自动映射一套 Function Calling 给 LLM 用！

通过 `BuildToolsBase<TBuilder>` + Source Generator（`CsTrees.MEAI.SourceGenerator`），自动将 `TBuilder` 中 `IBehaviourCatalog` 的工厂方法转换为工具调用方法。每次调用返回带 ASCII 树预览和 Blackboard 状态的 `ToolResult`，供 LLM 理解当前构建进度。

本包依赖 MEAI（`Microsoft.Extensions.AI.Abstractions`），你可以把它集成到 MEAI 或 MAF 项目中去。

## 快速开始

### 1. 安装

```
dotnet add package CsTrees.MEAI
```

### 2. 声明工具宿主类

继承 `BuildToolsBase<TBuilder>`，关联你的 TreeBuilder：

```csharp
using CsTrees.MEAI;

public partial class PizzaTreeTools : BuildToolsBase<PizzaBuilder>
{
    public PizzaTreeTools(PizzaBuilder builder) : base(builder) { }
}
```

Source Generator 会自动为 `PizzaBuilder` 中每个 `IBehaviourCatalog` 工厂方法生成对应的工具方法，并生成 `Tools` 属性（`Delegate[]`）聚合所有工具方法。

### 3. 使用

```csharp
var builder = new PizzaBuilder();
var tools = new PizzaTreeTools(builder);

// tools.Tools 即为所有可用工具的 Delegate[]，可注册到 AI Agent
// 生成的方法示例（由 SG 根据 Catalog 自动生成）：
tools.Selector("Root");           // 对应 Catalog 中的 Selector 工厂方法
tools.End();                      // 基类内置方法
tools.BuildTree();                // 构建并返回树结构
```

## 内置工具方法（`BuildToolsBase<TBuilder>` 提供）

| 方法 | 说明 |
|------|------|
| `End()` | 关闭当前打开的作用域，返回上一级 |
| `BuildTree()` | 构建行为树并返回 ASCII 树形结构，自动关闭剩余作用域 |
| `ShowTreeStatus()` | 查看当前构建状态及在建树的预览（只读） |
| `RunTree()` | 运行已构建的行为树若干次 tick，返回最终树形状态 |
| `ResetTree()` | 重置构建器到初始检查点，清空黑板与已构建的树 |

## SG 生成的方法

根据 `TBuilder` 中声明的 `IBehaviourCatalog` 工厂方法自动生成。每个生成方法的行为：

1. 检查 `builtTree` 状态（已构建则返回错误）
2. 委托调用 `builder` 上的同名方法
3. 返回 `ToolResult`（含确认消息 + ASCII 树预览 + Blackboard 状态）

方法的 `[Description]` 特性从 Catalog 工厂方法继承，供 AI Agent 理解工具用途。

## 集成示例：MEAI Agent

以下展示如何配合 `Microsoft.Extensions.AI` 的 `ChatClientAgent` 使用：

```csharp
using CsTrees.FluentBuilder;
using CsTrees.MEAI;
using Microsoft.Extensions.AI;

// 1. 定义行为目录（工厂方法会被 SG 扫描并生成对应工具方法）
public class PizzaCatalog : IBehaviourCatalog
{
    [Description("添加一个披萨制作动作作为叶节点")]
    public PizzaAction PizzaAction([Description("要添加的披萨动作类型")] PizzaActionType action)
        => new PizzaAction(action);

    [Description("添加预热烤箱行为，将烤箱代号写入 Blackboard")]
    public PreheatOven PreheatOven(string name,
        [Description("烤箱代号，如 \"A\"、\"B\"、\"C\"")] string ovenId,
        Blackboard blackboard) => new PreheatOven(name, ovenId, 400, blackboard);
}

// 2. 定义领域构建器（声明 IBehaviourCatalog 字段，SG 据此生成工具方法）
public partial class PizzaBuilder : TreeBuilder<PizzaBuilder>
{
    private readonly BasicCatalog basicCatalog = new();   // 标准节点（Sequence/Selector 等）
    private readonly PizzaCatalog pizzaCatalog = new();   // 自定义行为
}

// 3. 声明工具宿主（SG 为上述 Catalog 的所有工厂方法自动生成工具方法）
public partial class PizzaBuildTools : BuildToolsBase<PizzaBuilder>
{
    public PizzaBuildTools(PizzaBuilder builder) : base(builder) { }

    // 可额外添加自定义工具方法（返回 ToolResult），会自动纳入 Tools 数组
    [Description("列出所有可用的披萨制作动作")]
    public ToolResult ListPizzaActions() => new() { Message = "..." };
}

// 4. 创建实例并注册到 Agent
var builder = new PizzaBuilder().WithBlackboard(new Blackboard());
var tools = new PizzaBuildTools(builder);

var agent = new ChatClientAgent(
    iChatClient,
    instructions: "你是一个行为树构建助手...",
    name: "BehaviorTreeBuilder",
    // Tools 数组包含所有工具方法（基类 + SG 生成 + 自定义），统一转为 AIFunction
    tools: tools.Tools.Select(d => AIFunctionFactory.Create(d)).ToArray()
);

// 5. 交互
var session = await agent.CreateSessionAsync();
var history = new List<ChatMessage>();
history.Add(new ChatMessage(ChatRole.User, "帮我构建一个制作披萨的行为树"));
var response = await agent.RunAsync(history, session);
```

> **提示**：`Tools` 属性聚合了基类方法（End、BuildTree 等）、SG 生成的 Catalog 方法和宿主类自定义方法，统一以 `Delegate[]` 暴露。使用 `AIFunctionFactory.Create(d)` 逐个转换即可得到用于 MEAI 注册的对象。

## ToolResult

```csharp
public class ToolResult
{
    public string Message { get; set; }   // 操作确认消息
    public string? Tree { get; set; }     // ASCII 树渲染文本（null 表示无树可显示）
    public string? Blackboard { get; set; } // Blackboard 状态渲染文本（null 表示无数据）
}
```

MEAI 会将此对象序列化为 JSON 返回给 LLM。`Tree` 字段包含完整的 ASCII 树预览，LLM 可据此理解当前树的形状和构建进度。

## CompactResultChatClient

`IChatClient` 装饰器，用于降低 token 消耗。构建过程中每次工具调用返回的 `ToolResult` 都带有完整的 ASCII 树预览（`Tree` 字段），多轮对话的历史会累积大量重复的树文本。本装饰器在每次 API 调用前遍历历史，只保留**最后一个**含 `Tree` 字段的结果，将其余结果的 `Tree` 移除。

```csharp
using CsTrees.MEAI;
using Microsoft.Extensions.AI;

// 装饰你的 IChatClient 即可启用去重
IChatClient client = new CompactResultChatClient(iChatClient);
var agent = new ChatClientAgent(client, instructions: "...", tools: ...);
```


## 与 CsTrees 的关系

| 包 | 职责 |
|----|------|
| `CsTrees` | 行为树核心框架（节点、黑板、构建器） |
| `CsTrees.MEAI` | 将构建操作暴露为工具调用接口（本包） |
| `CsTrees.MEAI.SourceGenerator` | 源生成器，随本包一起分发 |

## 许可证

MIT
