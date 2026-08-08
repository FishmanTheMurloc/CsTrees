namespace CsTrees.Display;

/// <summary>
/// ASCII symbol configuration for <see cref="AsciiTreeRenderer"/>.
/// <para>
/// Override virtual properties to customise the symbols used in the rendered
/// tree. Override <see cref="FormatText"/> and <see cref="FormatStatus"/>
/// to add styling (e.g. ANSI colours, bold).
/// </para>
/// <para>
/// To create a coloured variant, subclass and override the formatting methods:
/// <code>
/// public class AnsiAsciiSymbols : AsciiSymbols
/// {
///     public override string FormatText(string text, bool isTip)
///         => isTip ? $"\x1b[1m{text}\x1b[0m" : text;
///
///     public override string FormatStatus(string symbol, Status status)
///         => status switch
///         {
///             Status.Success =&gt; $"\x1b[32m{symbol}\x1b[0m",
///             Status.Failure =&gt; $"\x1b[31m{symbol}\x1b[0m",
///             Status.Invalid =&gt; $"\x1b[33m{symbol}\x1b[0m",
///             Status.Running =&gt; $"\x1b[34m{symbol}\x1b[0m",
///             _ =&gt; symbol
///         };
/// }
/// </code>
/// </para>
/// </summary>
public class AsciiSymbols
{
    /// <summary>Symbol for a sequence with memory.</summary>
    public virtual string SequenceWithMemory => "{-}";

    /// <summary>Symbol for a sequence without memory.</summary>
    public virtual string SequenceWithoutMemory => "[-]";

    /// <summary>Symbol for a selector with memory.</summary>
    public virtual string SelectorWithMemory => "{o}";

    /// <summary>Symbol for a selector without memory.</summary>
    public virtual string SelectorWithoutMemory => "[o]";

    /// <summary>Symbol for a parallel composite.</summary>
    public virtual string Parallel => "/_/";

    /// <summary>Symbol for a decorator.</summary>
    public virtual string Decorator => "-^-";

    /// <summary>Symbol for a leaf behaviour.</summary>
    public virtual string Behaviour => "-->";

    /// <summary>Symbol for <see cref="Status.Success"/>.</summary>
    public virtual string StatusSuccess => "o";

    /// <summary>Symbol for <see cref="Status.Failure"/>.</summary>
    public virtual string StatusFailure => "x";

    /// <summary>Symbol for <see cref="Status.Invalid"/>.</summary>
    public virtual string StatusInvalid => "-";

    /// <summary>Symbol for <see cref="Status.Running"/>.</summary>
    public virtual string StatusRunning => "*";

    /// <summary>Placeholder text for collapsed subtrees.</summary>
    public virtual string Collapsed => "...";

    /// <summary>
    /// Get the symbol for a behaviour type string.
    /// Maps <see cref="BehaviourRenderInfo.BehaviourType"/> values to their
    /// corresponding symbol properties.
    /// </summary>
    /// <param name="behaviourType">One of the behaviour type strings defined by <see cref="BehaviourRenderInfo.BehaviourType"/>.</param>
    public virtual string GetBehaviourTypeSymbol(string behaviourType) => behaviourType switch
    {
        "parallel" => Parallel,
        "decorator" => Decorator,
        "sequence_with_memory" => SequenceWithMemory,
        "sequence_without_memory" => SequenceWithoutMemory,
        "selector_with_memory" => SelectorWithMemory,
        "selector_without_memory" => SelectorWithoutMemory,
        _ => Behaviour,
    };

    /// <summary>
    /// Get the symbol for a behaviour status.
    /// </summary>
    public virtual string GetStatusSymbol(Status status) => status switch
    {
        Status.Success => StatusSuccess,
        Status.Failure => StatusFailure,
        Status.Invalid => StatusInvalid,
        Status.Running => StatusRunning,
        _ => "?",
    };

    /// <summary>
    /// Apply text formatting (e.g. bold for tip behaviours).
    /// Override to add styling such as ANSI escape sequences.
    /// </summary>
    /// <param name="text">The text to format.</param>
    /// <param name="isTip">Whether this text belongs to a tip behaviour.</param>
    public virtual string FormatText(string text, bool isTip) => text;

    /// <summary>
    /// Apply status-specific formatting (e.g. ANSI colour codes).
    /// Override to add colouring to status symbols.
    /// </summary>
    /// <param name="symbol">The status symbol string.</param>
    /// <param name="status">The status value.</param>
    public virtual string FormatStatus(string symbol, Status status) => symbol;

    /// <summary>Default symbol set instance (plain text, no styling).</summary>
    public static AsciiSymbols Default { get; } = new();
}
