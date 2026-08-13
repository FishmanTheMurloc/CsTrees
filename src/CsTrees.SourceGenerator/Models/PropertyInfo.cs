using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace CsTrees.SourceGenerator.Models;

internal sealed class PropertyInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
    public string ValueTypeRef { get; set; } = string.Empty;
    public string DefaultKey { get; set; } = string.Empty;
    public string Access { get; set; } = "Write";
    public string ContainingClassName { get; set; } = string.Empty;
    public string ContainingNamespace { get; set; } = string.Empty;
    public bool IsPartial { get; set; }
    public bool IsBehaviourSubclass { get; set; }
    public bool HasValidType { get; set; }
    public bool HasGenerateBuilderExtension { get; set; }
    public Location Location { get; set; } = Location.None;

    /// <summary>
    /// Constructor parameter info collected from the containing class.
    /// Each inner list represents one constructor's parameters.
    /// </summary>
    public List<List<(string Type, string Name, string? DefaultValue)>> ConstructorParamsList { get; set; } = new();
}
