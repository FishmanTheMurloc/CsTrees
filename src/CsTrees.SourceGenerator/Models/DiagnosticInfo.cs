using Microsoft.CodeAnalysis;

namespace CsTrees.SourceGenerator.Models;

/// <summary>
/// 诊断信息的轻量载体（类名 + 位置）。
/// </summary>
internal sealed class DiagnosticInfo
{
    public string ClassName { get; set; } = string.Empty;
    public Location Location { get; set; } = Location.None;
}
