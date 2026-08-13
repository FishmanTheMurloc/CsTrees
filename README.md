# CsTrees [![NuGet Version](https://img.shields.io/nuget/v/CsTrees.svg?style=flat-square)](https://www.nuget.org/packages/CsTrees)

一个 .NET 行为树框架，基本设计复刻了 [py_trees](https://github.com/splintered-reality/py_trees)。

## 特性

- **复刻py_trees的节点类型和大部分API**：Behaviours、Composites和Decorators
- **基于C#改造的黑板系统**：类型安全的键值对共享状态，访问控制的源生成器
- **流式构建器**：基于栈的声明式 API，支持黑板作用域嵌套，可继承的泛型 CRTP 基类
- **行为源生成**：通过 `[GenerateTreeBuilderExtension]` 自动生成扩展方法，或通过 `IBehaviourCatalog` 构建领域特定构建器
- **扩展的显示**：除了自带ASCII渲染以外，可按需扩展；构建中也可输出预览

## 快速开始

### 直接构建行为树

```csharp
using CsTrees;
using CsTrees.Composites;
using CsTrees.Behaviours;

var tree = new Selector("Root", children: new[]
{
    new Sequence("Check & Act", children: new[]
    {
        new Success("Condition Check"),
        new Success("Execute Action")
    }),
    new Failure("Fallback")
});

await tree.TickOnce();
```

### 流式构建器

```csharp
using CsTrees.FluentBuilder;

var bb = new CsTrees.Blackboard.Blackboard();

var tree = new TreeBuilder()
    .Selector("Root")
        .Sequence("Check & Act")
            .WithBlackboard(bb)
                .MyCheck("Check")
                .MyBehaviour("Act")
            .End()
        .End()
        .Failure("Fallback")
    .End()
    .Build();
```

### 显示行为树

```csharp
using CsTrees.Display;

string ascii = Display.AsciiTree(tree, showStatus: true, showFeedbackMessage: true);
Console.WriteLine(ascii);
```

## 节点类型

CsTrees 的节点类型与 py_trees 基本一致，详细的节点说明请参阅 [py_trees 文档](https://py-trees.readthedocs.io/)。

其他复刻的功能点也请翻阅仓库代码并和原版比对。

## 黑板（Blackboard）

基本理念和 [py_trees 黑板](https://py-trees.readthedocs.io/en/devel/blackboards.html) 相同。

CsTrees 的黑板并不是单例的，且没有采用Client的设计。  
CsTrees 提供了两种方式与黑板交互：**手动注册端口**和**通过 `[BlackboardKey]` 特性自动生成**。

### 通过 `[BlackboardKey]` 特性声明端口（推荐）

在 `partial` 行为类上，使用 `[BlackboardKey]` 特性标记 `BehaviourKeyAccess<T>` 类型的属性即可。源生成器（CsTrees.SourceGenerator）会自动生成以下代码：

- **带黑板参数的构造函数重载** — 自动注册所有端口，无需手动调用 `GrantRead/GrantWrite`
- **`SetupPorts(Blackboard)` 方法** — 手动注册端口（可选）

> **注意**：TreeBuilder 扩展方法**不会自动生成**。需要额外标注 `[GenerateTreeBuilderExtension]` 特性才会生成（见下方说明）。

```csharp
using CsTrees;
using CsTrees.Blackboard;
using CsTrees.FluentBuilder;
using System.Threading.Tasks;

[GenerateTreeBuilderExtension]
public partial class DetectButton : Behaviour
{
    [BlackboardKey("btn_x", Access = Access.Write)]
    public BehaviourKeyAccess<int> X { get; private set; } = null!;

    [BlackboardKey("btn_y", Access = Access.Write)]
    public BehaviourKeyAccess<int> Y { get; private set; } = null!;

    // 构造函数需为 private，源生成器会生成带 Blackboard 参数的 public 重载
    private DetectButton(string name) : base(name) { }

    protected override async Task<Status> Update()
    {
        X.Set(42);
        Y.Set(99);
        return Status.Success;
    }
}
```

### `[GenerateTreeBuilderExtension]` 自动生成 TreeBuilder 扩展方法

在 Behaviour 子类上标注 `[GenerateTreeBuilderExtension]` 后，源生成器会为该类生成静态扩展方法类（如 `DetectButtonBuilderExtensions`），包含一个或多个扩展方法（对应每个 private 构造函数一个）。这些方法：

- 接收 `this TBuilder builder` 和 `string name` 参数
- 自动从构建器当前黑板作用域获取 `Blackboard` 并注入构造函数
- 支持可选的端口 key 覆盖参数（如 `xKey: "custom_x"`）

```csharp
// 生成的扩展方法使用方式
var tree = new TreeBuilder()
    .Sequence("Pipeline")
        .WithBlackboard(bb)
            .DetectButton("检测按钮")  // SG 生成的扩展方法，bb 自动注入
        .End()
    .End()
    .Build();
```

> **使用建议**：通用行为（如传感器检测、基础移动）使用 `[GenerateTreeBuilderExtension]` 生成全局扩展方法；业务预设行为通过 `IBehaviourCatalog` 构建领域构建器（见下方）。

### 手动注册端口

不依赖源生成器时，可以手动调用 `GrantRead`/`GrantWrite`/`GrantExclusiveWrite`：

```csharp
using CsTrees.Blackboard;

var bb = new Blackboard();

var readAccess = bb.GrantRead<int>(behaviour, "/sensor/value");
var writeAccess = bb.GrantWrite<string>(behaviour, "/actor/state");
var exclusiveAccess = bb.GrantExclusiveWrite<bool>(behaviour, "/locked");

// 在行为的 Update() 中
var value = readAccess.Get();
writeAccess.Set("active");
exclusiveAccess.Set(true);
readAccess.Unset();
```

## 流式构建器（TreeBuilder）

TreeBuilder 提供基于栈的声明式 API，是更流行的选择。

```csharp
using CsTrees.FluentBuilder;

var tree = new TreeBuilder()
    .Selector("Root")
        .Sequence("Check & Act")
            .Success("Condition")
            .Failure("Action")
        .End()
        .Failure("Fallback")
    .End()
    .Build();
```

### 配合黑板使用（推荐）

在大多数时刻我们只用到一块黑板，因此可以通过黑板作用域的设计来简化代码

通过 `WithBlackboard()` 为子节点注入黑板作用域。标注了 `[GenerateTreeBuilderExtension]` 的行为会生成 TreeBuilder 扩展方法，黑板会自动从当前作用域获取：

```csharp
var bb = new Blackboard();

var tree = new TreeBuilder()
    .Selector("Root")
        .Sequence("Pipeline")
            .WithBlackboard(bb)
                // 利用源生成器生成的扩展方法，可以在 WithBlackboard 作用域内省略 bb 的显式注入
                .DetectButton("检测按钮")
                .MoveTo("移动到目标")
            .End()
        .End()
    .End()
    .Build();
```

### 预览

`Preview()` 方法可以在构建过程中随时预览当前的树结构：

```csharp
var builder = new TreeBuilder()
    .Selector("Root")
        .Sequence("Part1")
            .Success("Step1");

// 预览当前状态（Part1 未 End）
var preview = builder.Preview();
Console.WriteLine(Display.AsciiTree(preview));

// 继续构建
builder
    .Success("Step2")
    .End()
    .End()
    .Build();
```

## 继承 TreeBuilder 构建领域特定构建器

TreeBuilder 基于 CRTP 模式实现为泛型基类 `TreeBuilder<TBuilder>`，支持继承以创建领域特定的行为树构建器。

### 基础继承

```csharp
public class MyBuilder : TreeBuilder<MyBuilder>
{
}

var tree = new MyBuilder()
    .Sequence("Root")
        .Success("Action")
    .End()
    .Build();
```

继承后，所有扩展方法返回的是派生类型 `MyBuilder`，可在继承链上继续添加自定义方法。

### 通过 `IBehaviourCatalog` 自动生成构建方法

对于业务预设行为树，推荐使用 `IBehaviourCatalog<TCatalog>` 模式。源生成器会扫描 Catalog 中的 public 工厂方法，自动为 TreeBuilder 子类生成对应的构建方法：

```csharp
// 1. 定义行为目录类
public class GameCatalog
{
    // 工厂方法：不含 Blackboard 参数
    public Behaviour MakePlayerIdle(string name) => new Idle(name);

    // 工厂方法：含 Blackboard 参数，生成的构建方法会自动注入作用域 bb
    public Behaviour MakeCollectItem(string name, Blackboard bb)
        => new CollectItem(name, bb);
}

// 2. 定义领域构建器
public partial class GameBuilder : TreeBuilder<GameBuilder>, IBehaviourCatalog<GameCatalog>
{
    public GameCatalog Catalog { get; } = new();
}

// 3. 使用（MakePlayerIdle 和 MakeCollectItem 由 SG 自动生成）
var tree = new GameBuilder()
    .Sequence("Gameplay")
        .MakePlayerIdle("待机")
        .MakeCollectItem("拾取物品")  // bb 自动注入
    .End()
    .Build();
```

Catalog 工厂方法的参数规则：

- 返回类型必须为 `Behaviour` 或其子类
- 如果参数中包含 `Blackboard` 类型，生成的构建方法会通过 `LeafWithBlackboard` 自动注入当前作用域的黑板
- 不含 `Blackboard` 参数的工厂方法，生成的构建方法通过 `Leaf` 直接添加节点
- 参数默认值会被保留到生成的方法签名中

> `[GenerateTreeBuilderExtension]` 和 `IBehaviourCatalog` 是两种互补的代码生成路径：前者生成**全局静态扩展方法**，适合通用行为；后者生成**实例方法**挂在领域构建器上，适合业务预设。

## 许可证

MIT
