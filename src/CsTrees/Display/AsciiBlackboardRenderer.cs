using System.Text;
using CsTrees.Blackboard;

namespace CsTrees.Display;

/// <summary>
/// Renders a blackboard as ASCII text.
/// <para>
/// Produces output like:
/// <example>
/// Blackboard Data
///     foo: bar
///     spaghetti: -
/// </example>
/// </para>
/// <para>
/// Customise the symbols by passing a subclassed <see cref="BlackboardSymbols"/>
/// instance.
/// </para>
/// </summary>
public sealed class AsciiBlackboardRenderer : BlackboardRenderer
{
    private readonly StringBuilder _sb = new();
    private readonly BlackboardSymbols _symbols;

    /// <summary>
    /// Create an ASCII blackboard renderer with the specified symbol set.
    /// </summary>
    /// <param name="symbols">
    /// Symbol configuration. Defaults to <see cref="BlackboardSymbols.Default"/>
    /// (plain text, no styling).
    /// </param>
    public AsciiBlackboardRenderer(BlackboardSymbols? symbols = null)
    {
        _symbols = symbols ?? BlackboardSymbols.Default;
    }

    /// <inheritdoc/>
    public override void Begin()
    {
        _sb.AppendLine(_symbols.FormatTitle("Blackboard Data"));
    }

    /// <summary>
    /// Render a single blackboard item as a line of ASCII text.
    /// <para>
    /// The line format is:
    /// <c>    Key: Value</c> if the key has a value,
    /// or <c>    Key: -</c> if the key is registered but not set.
    /// </para>
    /// </summary>
    /// <param name="item">The blackboard item to render.</param>
    public override void WriteItem(BlackboardItem item)
    {
        var keyText = _symbols.FormatKey(item.Key);

        if (item.HasValue)
        {
            var valueText = _symbols.FormatObject(item.Value);
            var formattedValue = _symbols.FormatValue(valueText);
            _sb.AppendLine($"    {keyText}{_symbols.Separator}{formattedValue}");
        }
        else
        {
            var notSetValue = _symbols.FormatNotSet(_symbols.NotSet);
            _sb.AppendLine($"    {keyText}{_symbols.Separator}{notSetValue}");
        }
    }

    /// <inheritdoc/>
    public override void End() { }

    /// <inheritdoc/>
    public override string GetResult() => _sb.ToString();
}