namespace CsTrees.Behaviours;

/// <summary>
/// Do nothing but tick over with <see cref="Status.Failure"/>.
/// <para>
/// Corresponds to <c>py_trees.behaviours.Failure</c>.
/// </para>
/// </summary>
public class Failure : Behaviour
{
    /// <summary>
    /// Create a new Failure behaviour.
    /// </summary>
    /// <param name="name">Name of the behaviour.</param>
    public Failure(string name) : base(name) { }

    /// <summary>
    /// Always return <see cref="Status.Failure"/>.
    /// </summary>
    protected async override Task<Status> Update()
    {
        FeedbackMessage = "failure";
        return await Task.FromResult(Status.Failure);
    }
}
