namespace CsTrees.Behaviours;

/// <summary>
/// Do nothing but tick over with <see cref="Status.Running"/>.
/// <para>
/// Corresponds to <c>py_trees.behaviours.Running</c>.
/// </para>
/// </summary>
public class Running : Behaviour
{
    /// <summary>
    /// Create a new Running behaviour.
    /// </summary>
    /// <param name="name">Name of the behaviour.</param>
    public Running(string name) : base(name) { }

    /// <summary>
    /// Always return <see cref="Status.Running"/>.
    /// </summary>
    protected async override Task<Status> Update()
    {
        FeedbackMessage = "running";
        return await Task.FromResult(Status.Running);
    }
}
