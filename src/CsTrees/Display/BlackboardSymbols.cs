namespace CsTrees.Display;

/// <summary>
/// ASCII symbol configuration for <see cref="AsciiBlackboardRenderer"/>.
/// <para>
/// Override virtual properties to customise the symbols used in the rendered
/// blackboard. Override formatting methods to add styling (e.g. ANSI colours).
/// </para>
/// </summary>
public class BlackboardSymbols
{
    /// <summary>Separator between key and value (e.g. ": ").</summary>
    public virtual string Separator => ": ";

    /// <summary>Symbol for a key that has not been set.</summary>
    public virtual string NotSet => "-";

    /// <summary>
    /// Format the title line.
    /// Override to add styling such as ANSI escape sequences.
    /// </summary>
    /// <param name="title">The title text.</param>
    public virtual string FormatTitle(string title) => title;

    /// <summary>
    /// Format a key name.
    /// Override to add styling such as ANSI escape sequences.
    /// </summary>
    /// <param name="key">The key name.</param>
    public virtual string FormatKey(string key) => key;

    /// <summary>
    /// Format a value.
    /// Override to add styling such as ANSI escape sequences.
    /// </summary>
    /// <param name="value">The value string.</param>
    public virtual string FormatValue(string value) => value;

    /// <summary>
    /// Format a complex object value.
    /// Override to customise serialization (e.g. JSON, XML) for complex objects.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The formatted string representation.</returns>
    public virtual string FormatObject(object? value)
    {
        return value?.ToString() ?? "null";
    }

    /// <summary>
    /// Format the "not set" indicator.
    /// Override to add styling such as ANSI escape sequences.
    /// </summary>
    /// <param name="notSet">The "not set" symbol.</param>
    public virtual string FormatNotSet(string notSet) => notSet;

    /// <summary>Default symbol set instance (plain text, no styling).</summary>
    public static BlackboardSymbols Default { get; } = new();
}