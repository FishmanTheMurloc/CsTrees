using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace CsTrees.SourceGenerator.Models;

internal sealed class ClassInfo
{
    public string ClassName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<PropertyInfo> Properties { get; set; } = new();
    public List<PropertyInfo> InvalidProperties { get; set; } = new();
    public List<List<(string Type, string Name, string?)>> Constructors { get; set; } = new();
    public bool IsPartial { get; set; }
    public bool IsBehaviourSubclass { get; set; }
    public bool HasGenerateBuilderExtension { get; set; }
    public Location Location { get; set; } = Location.None;
}
