namespace CsTrees.Behaviours;

/// <summary>
/// Crash test dummy used for anything dangerous.
/// Always ticks over with <see cref="Status.Running"/>.
/// <para>
/// Corresponds to <c>py_trees.behaviours.Dummy</c>.
/// </para>
/// </summary>
public class Dummy : Behaviour
{
    /// <summary>
    /// Create a new Dummy behaviour.
    /// </summary>
    /// <param name="name">Name of the behaviour.</param>
    public Dummy(string name) : base(name) { }

    /// <summary>
    /// Always return <see cref="Status.Running"/>.
    /// </summary>
    protected async override Task<Status> Update()
    {
        FeedbackMessage = "crash test dummy";
        return await Task.FromResult(Status.Running);
    }
}
