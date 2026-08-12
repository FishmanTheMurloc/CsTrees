# CsTrees [![NuGet Version](https://img.shields.io/nuget/v/CsTrees.svg?style=flat-square)](https://www.nuget.org/packages/CsTrees)

一个 .NET 行为树框架，基本设计复刻了 [py_trees](https://github.com/splintered-reality/py_trees)。

## 特性

- **复刻py_trees的节点类型和大部分API**：Behaviours、Composites和Decorators
- **基于C#改造的黑板系统**：类型安全的键值对共享状态，访问控制的源生成器
- **流式构建器**：基于栈的声明式 API，支持黑板作用域嵌套，流式构建方法的源生成器
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

var tree = TreeBuilder.Create()
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
- **TreeBuilder 扩展方法** — 可直接在流式构建器中使用该行为，黑板自动注入
- **`GetPortDeclarations()` 静态方法** — 返回该行为声明的所有端口元数据（键名、类型、访问级别），但 `PortDeclaration` 是待定功能。


```csharp
using CsTrees;
using CsTrees.Blackboard;
using System.Threading.Tasks;

public partial class DetectButton : Behaviour
{
    [BlackboardKey("btn_x", Access = Access.Write)]
    public BehaviourKeyAccess<int> X { get; private set; } = null!;

    [BlackboardKey("btn_y", Access = Access.Write)]
    public BehaviourKeyAccess<int> Y { get; private set; } = null!;

    public DetectButton(string name) : base(name) { }

    public override Task<Status> Update()
    {
        X.Set(42);
        Y.Set(99);
        return Task.FromResult(Status.Success);
    }
}
```

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

var tree = TreeBuilder.Create()
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

通过 `WithBlackboard()` 为子节点注入黑板作用域。源生成器为带 `[BlackboardKey]` 的行为自动生成了 TreeBuilder 扩展方法，黑板会自动从当前作用域获取：

```csharp
var bb = new Blackboard();

var tree = TreeBuilder.Create()
    .Selector("Root")
        .Sequence("Pipeline")
            .WithBlackboard(bb)
                // 利用源生成器生成的扩展方法，可以在WithBlackboard作用域内省略bb的显式注入
                .DetectButton("检测按钮")
                .MoveTo("移动到目标")
            .End()
        .End()
    .End()
    .Build();
```

### 预览（推荐）

`Preview()` 方法可以在构建过程中随时预览当前的树结构：

```csharp
var builder = TreeBuilder.Create()
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

## 许可证

MIT
