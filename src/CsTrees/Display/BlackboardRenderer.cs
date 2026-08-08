using CsTrees.Blackboard;

namespace CsTrees.Display;

/// <summary>
/// Abstract base class for blackboard renderers.
/// <para>
/// Renderers receive <see cref="BlackboardItem"/> instances during rendering
/// and produce output in their own format.
/// Subclass this to create custom renderers (e.g. JSON, HTML, Markdown).
/// </para>
/// </summary>
public abstract class BlackboardRenderer
{
    /// <summary>
    /// Called once before rendering begins.
    /// Override to perform initialisation (e.g. opening tags, headers).
    /// </summary>
    public abstract void Begin();

    /// <summary>
    /// Called once for each blackboard item.
    /// </summary>
    /// <param name="item">The blackboard item to render.</param>
    public abstract void WriteItem(BlackboardItem item);

    /// <summary>
    /// Called once after rendering ends.
    /// Override to perform finalisation (e.g. closing tags, footers).
    /// </summary>
    public abstract void End();

    /// <summary>
    /// Return the final rendered output.
    /// Called after <see cref="End"/>.
    /// </summary>
    public abstract string GetResult();
}