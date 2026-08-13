using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace CsTrees.SourceGenerator.Models;

/// <summary>
/// 描述一个 partial TreeBuilder 子类及其待生成的预设构建方法。
/// </summary>
internal sealed class TreeBuilderSubclassInfo
{
    public string ClassName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<DeclMethodInfo> Methods { get; set; } = new();
    public Location Location { get; set; } = Location.None;
}

/// <summary>
/// TCatalog 中一个 public 工厂方法（返回 Behaviour 子类）的收集结果。
/// Parameters 保留所有参数（含 blackboard，用 IsBlackboard 标记），
/// 以便生成签名时跳过 blackboard、生成调用时按原位置传 bb。
/// </summary>
internal sealed class DeclMethodInfo
{
    public string MethodName { get; set; } = string.Empty;
    public List<ParamInfo> Parameters { get; set; } = new();
    public bool HasBlackboard => Parameters.Exists(p => p.IsBlackboard);
}

internal sealed class ParamInfo
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DefaultValue { get; set; }
    public bool IsBlackboard { get; set; }
}
