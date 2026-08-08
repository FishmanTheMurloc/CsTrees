using System.Text;
using CsTrees.Blackboard;

namespace CsTrees.Display;

/// <summary>
/// Renders an activity stream as ASCII text.
/// <para>
/// Produces output like:
/// <example>
/// Blackboard Activity Stream
///     count:  INITIALISED | Counter   |-> 1
///     count:  WRITE       | Counter   |-> 2
///     result: READ        | Processor |&lt;- 2
/// </example>
/// </para>
/// <para>
/// Column widths are computed automatically based on the widest key
/// and client name in the stream. The value column width is derived
/// from <see cref="ActivityStreamSymbols.TotalLineLength"/>.
/// </para>
/// <para>
/// Customise the symbols by passing a subclassed
/// <see cref="ActivityStreamSymbols"/> instance.
/// </para>
/// </summary>
public sealed class AsciiActivityStreamRenderer : ActivityStreamRenderer
{
    private readonly StringBuilder _sb = new();
    private readonly ActivityStreamSymbols _symbols;
    private readonly List<ActivityItem> _items = new();

    /// <summary>
    /// Create an ASCII activity stream renderer with the specified symbol set.
    /// </summary>
    /// <param name="symbols">
    /// Symbol configuration. Defaults to <see cref="ActivityStreamSymbols.Default"/>
    /// (plain text, no styling).
    /// </param>
    public AsciiActivityStreamRenderer(ActivityStreamSymbols? symbols = null)
    {
        _symbols = symbols ?? ActivityStreamSymbols.Default;
    }

    /// <summary>
    /// Write the title line if <see cref="ActivityStreamRenderer.ShowTitle"/> is <c>true</c>.
    /// </summary>
    public override void Begin()
    {
        if (ShowTitle)
        {
            _sb.Append(' ', Indent);
            _sb.AppendLine(_symbols.FormatTitle(_symbols.Title));
        }
    }

    /// <summary>
    /// Buffer an activity item for later rendering.
    /// <para>
    /// Items are buffered because column widths must be computed from
    /// all items before any can be rendered. Actual output is produced
    /// in <see cref="End"/>.
    /// </para>
    /// </summary>
    /// <param name="item">The activity item to render.</param>
    public override void WriteItem(ActivityItem item)
    {
        _items.Add(item);
    }

    /// <summary>
    /// Compute column widths from all buffered items and render them.
    /// </summary>
    public override void End()
    {
        if (_items.Count == 0)
            return;

        // Compute column widths
        var keyWidth = 0;
        var clientWidth = 0;
        foreach (var item in _items)
        {
            if (item.Key.Length > keyWidth)
                keyWidth = item.Key.Length;
            if (item.BehaviourName.Length > clientWidth)
                clientWidth = item.BehaviourName.Length;
        }
        clientWidth = Math.Min(clientWidth, _symbols.MaxClientWidth);

        // Type column width = longest activity type string (ACCESS_DENIED)
        var typeWidth = _symbols.GetActivityTypeString(ActivityType.AccessDenied).Length;

        // Value column width = remaining space on the line
        var valueWidth = _symbols.TotalLineLength - keyWidth - 3 - typeWidth - 3 - clientWidth - 3;
        if (valueWidth < 10)
            valueWidth = 10;

        var innerIndent = 4 + Indent;

        foreach (var item in _items)
        {
            // Key column (left-aligned, padded to keyWidth, with trailing colon)
            _sb.Append(' ', innerIndent);
            _sb.Append(_symbols.FormatKey(item.Key.PadRight(keyWidth + 1) + ":"));
            _sb.Append(' ');

            // Activity type column
            var typeStr = _symbols.GetActivityTypeString(item.ActivityType);
            _sb.Append(_symbols.FormatActivityType(typeStr.PadRight(typeWidth)));
            _sb.Append(' ');

            // Separator
            _sb.Append('|');
            _sb.Append(' ');

            // Client/behaviour name column (truncated and padded)
            var clientName = Truncate(item.BehaviourName.Replace("\n", "_"), clientWidth);
            _sb.Append(_symbols.FormatClientName(clientName.PadRight(clientWidth)));
            _sb.Append(' ');

            // Separator
            _sb.Append('|');
            _sb.Append(' ');

            // Arrow and value (varies by activity type)
            switch (item.ActivityType)
            {
                case ActivityType.Read:
                    _sb.Append(_symbols.FormatArrow(_symbols.LeftArrow));
                    _sb.Append(' ');
                    _sb.AppendLine(_symbols.FormatValue(Truncate(item.CurrentValue?.ToString() ?? "", valueWidth)));
                    break;

                case ActivityType.Write:
                case ActivityType.Initialised:
                    _sb.Append(_symbols.FormatArrow(_symbols.RightArrow));
                    _sb.Append(' ');
                    _sb.AppendLine(_symbols.FormatValue(Truncate(item.CurrentValue?.ToString() ?? "", valueWidth)));
                    break;

                case ActivityType.Accessed:
                    _sb.Append(_symbols.FormatArrow(_symbols.LeftRightArrow));
                    _sb.Append(' ');
                    _sb.AppendLine(_symbols.FormatValue(Truncate(item.CurrentValue?.ToString() ?? "", valueWidth)));
                    break;

                case ActivityType.AccessDenied:
                    _sb.Append(_symbols.AccessDeniedSymbol);
                    _sb.Append(' ');
                    _sb.AppendLine("client has no read/write access");
                    break;

                case ActivityType.NoKey:
                    _sb.Append(_symbols.AccessDeniedSymbol);
                    _sb.Append(' ');
                    _sb.AppendLine("key does not yet exist");
                    break;

                case ActivityType.NoOverwrite:
                    _sb.Append(_symbols.NoOverwriteSymbol);
                    _sb.Append(' ');
                    _sb.AppendLine(_symbols.FormatValue(Truncate(item.CurrentValue?.ToString() ?? "", valueWidth)));
                    break;

                case ActivityType.Unset:
                    _sb.AppendLine();
                    break;

                default:
                    _sb.AppendLine("unknown operation");
                    break;
            }
        }
    }

    /// <inheritdoc/>
    public override string GetResult() => _sb.ToString();

    /// <summary>
    /// Truncate a string to a maximum length, appending "..." if truncated.
    /// </summary>
    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength)
            return value;
        if (maxLength <= 3)
            return value.Substring(0, maxLength);
        return value.Substring(0, maxLength - 3) + "...";
    }
}
