using System.Collections.Generic;
using System.Text;
using CsTrees.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace CsTrees.SourceGenerator.Emitters;

internal static class CodeGenHelpers
{
    internal static void AppendPortParams(StringBuilder sb, List<PropertyInfo> props, string prefix = "            ")
    {
        foreach (var prop in props)
        {
            var paramName = ToCamelCase(prop.PropertyName) + "Key";
            sb.AppendLine(",");
            sb.Append($"{prefix}string? {paramName} = null");
        }
    }

    internal static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    internal static string? FormatDefaultValue(object? value, ITypeSymbol paramType)
    {
        if (value is null)
        {
            // Check if the type is nullable (reference type or nullable value type)
            var isNullable = paramType.IsReferenceType
                || (paramType is ITypeSymbol ts && ts.NullableAnnotation == NullableAnnotation.Annotated);
            if (isNullable)
                return "null";
            return null;
        }

        return value switch
        {
            string s => $"\"{EscapeString(s)}\"",
            bool b => b ? "true" : "false",
            char c => $"'{c}'",
            byte => value.ToString(),
            sbyte => value.ToString(),
            short => value.ToString(),
            ushort => value.ToString(),
            int i => $"{i}",
            uint => value.ToString(),
            long l => $"{l}L",
            ulong ul => $"{ul}UL",
            float f => $"{f}f",
            double d => $"{d}",
            decimal m => $"{m}m",
            null => "null",
            _ => null
        };
    }

    internal static string EscapeString(string? s)
    {
        if (s is null)
            return string.Empty;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
