using System.Text;

namespace CsTrees.Display;

/// <summary>
/// Renders a behaviour tree as an ASCII text tree.
/// <para>
/// Produces output like:
/// <example>
/// [-] Sequence [o]
///     --> Action 1 [o]
///     --> Action 2 [*] -- waiting for response
///     --> Action 3
/// </example>
/// </para>
/// <para>
/// Customise the symbols by passing a subclassed <see cref="AsciiSymbols"/>
/// instance. For ANSI colour support, see the example in
/// <see cref="AsciiSymbols"/>.
/// </para>
/// </summary>
public sealed class AsciiTreeRenderer : TreeRenderer
{
    private readonly StringBuilder _sb = new();
    private readonly AsciiSymbols _symbols;

    /// <summary>
    /// Create an ASCII tree renderer with the specified symbol set.
    /// </summary>
    /// <param name="symbols">
    /// Symbol configuration. Defaults to <see cref="AsciiSymbols.Default"/>
    /// (plain text, no styling).
    /// </param>
    public AsciiTreeRenderer(AsciiSymbols? symbols = null)
    {
        _symbols = symbols ?? AsciiSymbols.Default;
    }

    /// <inheritdoc/>
    public override void Begin() { }

    /// <summary>
    /// Render a single behaviour as a line of ASCII text.
    /// <para>
    /// The line format depends on the behaviour's visit state:
    /// <list type="bullet">
    ///   <item>Visited or <see cref="TreeRenderer.ShowStatus"/>:
    ///     <c>Indent TypeSymbol Name [StatusSymbol] -- FeedbackMessage</c></item>
    ///   <item>Previously running but not currently visited:
    ///     <c>Indent TypeSymbol Name [StatusSymbol]</c></item>
    ///   <item>Not visited: <c>Indent TypeSymbol Name</c></item>
    /// </list>
    /// Tip behaviours are passed through <see cref="AsciiSymbols.FormatText"/>
    /// for potential highlighting.
    /// </para>
    /// <para>
    /// If <see cref="BehaviourRenderInfo.ChildrenCollapsed"/> is <c>true</c>,
    /// a placeholder line (<see cref="AsciiSymbols.Collapsed"/>) is appended
    /// at the next indentation level.
    /// </para>
    /// </summary>
    /// <param name="info">Structured information about the current behaviour.</param>
    public override void WriteBehaviour(BehaviourRenderInfo info)
    {
        var tip = info.IsTip;

        // Indent
        _sb.Append(' ', 4 * info.Depth);

        // Type symbol
        var typeSymbol = _symbols.GetBehaviourTypeSymbol(info.BehaviourType);
        _sb.Append(_symbols.FormatText(typeSymbol, tip));
        _sb.Append(' ');

        // Name and status
        if (ShowStatus || info.Visited)
        {
            var name = info.Behaviour.Name.Replace("\n", " ");
            _sb.Append(_symbols.FormatText($"{name} [", tip));
            var statusSymbol = _symbols.GetStatusSymbol(info.Behaviour.Status);
            var formattedStatus = _symbols.FormatStatus(statusSymbol, info.Behaviour.Status);
            _sb.Append(_symbols.FormatText(formattedStatus, tip));
            var feedback = string.IsNullOrEmpty(info.Behaviour.FeedbackMessage)
                ? "]"
                : $"] -- {info.Behaviour.FeedbackMessage}";
            _sb.Append(_symbols.FormatText(feedback, tip));
        }
        else if (info.PreviouslyRunning)
        {
            var name = info.Behaviour.Name.Replace("\n", " ");
            _sb.Append(_symbols.FormatText($"{name} [", tip));
            var statusSymbol = _symbols.GetStatusSymbol(info.Behaviour.Status);
            var formattedStatus = _symbols.FormatStatus(statusSymbol, info.Behaviour.Status);
            _sb.Append(_symbols.FormatText(formattedStatus, tip));
            _sb.Append(_symbols.FormatText("]", tip));
        }
        else
        {
            var name = info.Behaviour.Name.Replace("\n", " ");
            _sb.Append(_symbols.FormatText(name, tip));
        }

        _sb.AppendLine();

        // Collapsed children placeholder
        if (info.ChildrenCollapsed)
        {
            _sb.Append(' ', 4 * (info.Depth + 1));
            _sb.AppendLine(_symbols.Collapsed);
        }
    }

    /// <inheritdoc/>
    public override void End() { }

    /// <inheritdoc/>
    public override string GetResult() => _sb.ToString();
}
