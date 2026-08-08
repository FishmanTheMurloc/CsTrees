namespace CsTrees.Decorators;

/// <summary>
/// Repeat the decorated child up to a specified number of consecutive successes.
/// <para>
/// <see cref="Status.Success"/> from the child is treated as <see cref="Status.Running"/>
/// until the specified number of consecutive successes is reached, at which point
/// this decorator returns <see cref="Status.Success"/>.
/// <see cref="Status.Failure"/> from the child is always <see cref="Status.Failure"/>.
/// </para>
/// </summary>
public class Repeat : Decorator
{
    /// <summary>
    /// Number of consecutive successes required. Use -1 to repeat indefinitely.
    /// </summary>
    public int NumSuccess { get; }

    private int _successCount;

    /// <summary>
    /// Create a new Repeat decorator.
    /// </summary>
    /// <param name="name">Name of the decorator.</param>
    /// <param name="child">The child behaviour to decorate.</param>
    /// <param name="numSuccess">Number of consecutive successes required. Use -1 to repeat indefinitely.</param>
    public Repeat(string name, Behaviour child, int numSuccess) : base(name, child)
    {
        NumSuccess = numSuccess;
    }

    /// <summary>
    /// Reset the currently registered number of successes.
    /// </summary>
    protected override void Initialize()
    {
        _successCount = 0;
    }

    /// <summary>
    /// Repeat until the Nth consecutive success.
    /// </summary>
    protected async override Task<Status> Update()
    {
        if (Decorated.Status == Status.Failure)
        {
            FeedbackMessage = $"failed, aborting [status: {_successCount} success from {NumSuccess}]";
            return await Task.FromResult(Status.Failure);
        }
        if (Decorated.Status == Status.Success)
        {
            _successCount++;
            FeedbackMessage = $"success [status: {_successCount} success from {NumSuccess}]";
            if (_successCount == NumSuccess)
                return await Task.FromResult(Status.Success);
            return await Task.FromResult(Status.Running);
        }
        // Running
        FeedbackMessage = $"running [status: {_successCount} success from {NumSuccess}]";
        return await Task.FromResult(Status.Running);
    }
}
