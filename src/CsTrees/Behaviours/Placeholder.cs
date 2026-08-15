namespace CsTrees.Behaviours;

/// <summary>
/// Construction placeholder indicating an incomplete position in the behaviour tree.
/// Always returns <see cref="Status.Invalid"/> to signal the node has not been defined yet.
/// <para>
/// Used by <see cref="FluentBuilder.TreeBuilder&lt;TBuilder&gt;.Preview"/> to represent
/// the current insertion point in an in-progress tree build.
/// </para>
/// </summary>
public class Placeholder : Behaviour
{
    /// <summary>
    /// Create a new Placeholder behaviour.
    /// </summary>
    /// <param name="name">Name of the behaviour, defaults to "...".</param>
    public Placeholder(string name = "...") : base(name) { }

    /// <summary>
    /// Always return <see cref="Status.Invalid"/> to signal the node has not been defined yet.
    /// </summary>
    protected async override Task<Status> Update()
    {
        FeedbackMessage = "under construction";
        return await Task.FromResult(Status.Invalid);
    }
}
