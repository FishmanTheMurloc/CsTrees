using CsTrees.Blackboard;

namespace CsTrees.Display;

/// <summary>
/// ASCII symbol configuration for <see cref="AsciiActivityStreamRenderer"/>.
/// <para>
/// Override virtual properties to customise the symbols used in the rendered
/// activity stream. Override formatting methods to add styling (e.g. ANSI colours).
/// </para>
/// </summary>
public class ActivityStreamSymbols
{
    /// <summary>Symbol for read operations (left arrow).</summary>
    public virtual string LeftArrow => "<-";

    /// <summary>Symbol for write/initialise operations (right arrow).</summary>
    public virtual string RightArrow => "->";

    /// <summary>Symbol for access operations (bidirectional arrow).</summary>
    public virtual string LeftRightArrow => "<->";

    /// <summary>Symbol for access denied / no key (cross mark).</summary>
    public virtual string AccessDeniedSymbol => "x";

    /// <summary>Symbol for no-overwrite operations.</summary>
    public virtual string NoOverwriteSymbol => "#";

    /// <summary>Title text for the activity stream output.</summary>
    public virtual string Title => "Blackboard Activity Stream";

    /// <summary>Maximum width for the client/behaviour name column.</summary>
    public virtual int MaxClientWidth => 20;

    /// <summary>Target total line length for computing value column width.</summary>
    public virtual int TotalLineLength => 80;

    /// <summary>
    /// Get the display string for an <see cref="ActivityType"/>.
    /// Override to customise how activity types are rendered in the output.
    /// </summary>
    /// <param name="activityType">The activity type to format.</param>
    public virtual string GetActivityTypeString(ActivityType activityType) => activityType switch
    {
        ActivityType.Read => "READ",
        ActivityType.Write => "WRITE",
        ActivityType.Initialised => "INITIALISED",
        ActivityType.Accessed => "ACCESSED",
        ActivityType.NoKey => "NO_KEY",
        ActivityType.AccessDenied => "ACCESS_DENIED",
        ActivityType.NoOverwrite => "NO_OVERWRITE",
        ActivityType.Unset => "UNSET",
        _ => "UNKNOWN"
    };

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
    /// Format an activity type string.
    /// Override to add styling such as ANSI escape sequences.
    /// </summary>
    /// <param name="activityType">The activity type display string.</param>
    public virtual string FormatActivityType(string activityType) => activityType;

    /// <summary>
    /// Format a client/behaviour name.
    /// Override to add styling such as ANSI escape sequences.
    /// </summary>
    /// <param name="clientName">The client name display string.</param>
    public virtual string FormatClientName(string clientName) => clientName;

    /// <summary>
    /// Format a direction arrow symbol.
    /// Override to add styling such as ANSI escape sequences.
    /// </summary>
    /// <param name="arrow">The arrow symbol.</param>
    public virtual string FormatArrow(string arrow) => arrow;

    /// <summary>
    /// Format a value.
    /// Override to add styling such as ANSI escape sequences.
    /// </summary>
    /// <param name="value">The value string.</param>
    public virtual string FormatValue(string value) => value;

    /// <summary>Default symbol set instance (plain text, no styling).</summary>
    public static ActivityStreamSymbols Default { get; } = new();
}
