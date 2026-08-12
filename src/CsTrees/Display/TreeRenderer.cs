namespace CsTrees.Display;

/// <summary>
/// Abstract base class for behaviour tree renderers.
/// <para>
/// Renderers receive structured behaviour information (<see cref="BehaviourRenderInfo"/>)
/// during tree traversal and produce output in their own format.
/// Subclass this to create custom renderers (e.g. JSON, HTML, Markdown).
/// </para>
/// </summary>
public abstract class TreeRenderer
{
    /// <summary>
    /// Whether to always show status for every behaviour, not just visited
    /// ones. Set by <see cref="Display.RenderTree"/> before traversal begins.
    /// </summary>
    public bool ShowStatus { get; set; }

    /// <summary>
    /// Whether to always show feedback message for every behaviour, not just
    /// visited ones. Set by <see cref="Display.RenderTree"/> before traversal
    /// begins.
    /// </summary>
    public bool ShowFeedbackMessage { get; set; }

    /// <summary>
    /// Called once before the tree traversal begins.
    /// Override to perform initialisation (e.g. opening tags, headers).
    /// </summary>
    public abstract void Begin();

    /// <summary>
    /// Called once for each behaviour during the traversal, in depth-first order.
    /// <para>
    /// All traversal decisions have already been made — the <see cref="BehaviourRenderInfo"/>
    /// contains everything the renderer needs to know about the behaviour.
    /// </para>
    /// </summary>
    /// <param name="info">Structured information about the current behaviour.</param>
    public abstract void WriteBehaviour(BehaviourRenderInfo info);

    /// <summary>
    /// Called once after the tree traversal ends.
    /// Override to perform finalisation (e.g. closing tags, footers).
    /// </summary>
    public abstract void End();

    /// <summary>
    /// Return the final rendered output.
    /// Called after <see cref="End"/>.
    /// </summary>
    public abstract string GetResult();
}
