using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace CsTrees.MEAI.SourceGenerator.Models;

/// <summary>
/// 描述一个继承 BuildToolsBase&lt;TBuilder&gt; 的 partial 类及其待生成的工具调用方法。
/// </summary>
internal sealed class BuildToolsClassInfo
{
    public string ClassName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public bool IsPartial { get; set; }
    public List<ToolMethodInfo> Methods { get; set; } = new();
    /// <summary>基类 BuildToolsBase&lt;TBuilder&gt; 直接声明的公共工具方法名（如 End、BuildTree）。</summary>
    public List<string> BaseToolMethods { get; set; } = new();
    /// <summary>宿主类自身直接声明的公共工具方法名（返回 ToolResult/Task&lt;ToolResult&gt;），与基类、目录方法一并生成 Tools 数组。</summary>
    public List<string> OwnToolMethods { get; set; } = new();
    /// <summary>同名工厂方法冲突（方法名 + 位置），用于报告 CSTM002。</summary>
    public List<(string MethodName, Location Location)> DuplicateMethods { get; set; } = new();
    /// <summary>缺少 [Description] 的方法（方法名 + 位置），用于报告 CSTM003。</summary>
    public List<(string MethodName, Location Location)> MissingDescriptions { get; set; } = new();
    public Location Location { get; set; } = Location.None;
}

internal enum ToolNodeType
{
    Leaf,
    Composite,
    Decorator
}

/// <summary>
/// 一个工具调用方法的生成信息，来源于 Builder 的某个 IBehaviourCatalog 工厂方法。
/// Parameters 已剔除 blackboard、children、child（由 Builder 作用域机制处理）。
/// </summary>
internal sealed class ToolMethodInfo
{
    public string MethodName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ToolNodeType NodeType { get; set; } = ToolNodeType.Leaf;
    public List<ToolParamInfo> Parameters { get; set; } = new();
    public Location Location { get; set; } = Location.None;
}

internal sealed class ToolParamInfo
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
}
