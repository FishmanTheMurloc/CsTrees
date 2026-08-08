using CsTrees.Blackboard;

namespace CsTrees.Display;

/// <summary>
/// Abstract base class for activity stream renderers.
/// <para>
/// Renderers receive <see cref="ActivityItem"/> instances during rendering
/// and produce output in their own format.
/// Subclass this to create custom renderers (e.g. JSON, HTML, Markdown).
/// </para>
/// </summary>
public abstract class ActivityStreamRenderer
{
    /// <summary>
    /// Whether to include the title line in the output.
    /// Set by <see cref="Display.RenderActivityStream"/> before rendering begins.
    /// </summary>
    public bool ShowTitle { get; set; } = true;

    /// <summary>
    /// The number of indentation levels to start at.
    /// Set by <see cref="Display.RenderActivityStream"/> before rendering begins.
    /// </summary>
    public int Indent { get; set; }

    /// <summary>
    /// Called once before rendering begins.
    /// Override to perform initialisation (e.g. opening tags, headers).
    /// </summary>
    public abstract void Begin();

    /// <summary>
    /// Called once for each activity item.
    /// </summary>
    /// <param name="item">The activity item to render.</param>
    public abstract void WriteItem(ActivityItem item);

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
