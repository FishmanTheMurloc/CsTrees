namespace CsTrees.Behaviours;

/// <summary>
/// Do nothing but tick over with <see cref="Status.Success"/>.
/// <para>
/// Corresponds to <c>py_trees.behaviours.Success</c>.
/// </para>
/// </summary>
public class Success : Behaviour
{
    /// <summary>
    /// Create a new Success behaviour.
    /// </summary>
    /// <param name="name">Name of the behaviour.</param>
    public Success(string name) : base(name) { }

    /// <summary>
    /// Always return <see cref="Status.Success"/>.
    /// </summary>
    protected async override Task<Status> Update()
    {
        FeedbackMessage = "success";
        return await Task.FromResult(Status.Success);
    }
}
