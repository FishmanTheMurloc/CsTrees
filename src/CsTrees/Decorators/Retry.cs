namespace CsTrees.Decorators;

/// <summary>
/// Keep trying — retries the decorated child up to a specified number of times on failure.
/// <para>
/// <see cref="Status.Failure"/> from the child is treated as <see cref="Status.Running"/>
/// until the maximum number of permitted failures is reached, at which point
/// this decorator returns <see cref="Status.Failure"/>.
/// <see cref="Status.Success"/> from the child is always <see cref="Status.Success"/>.
/// </para>
/// </summary>
public class Retry : Decorator
{
    /// <summary>
    /// Maximum number of permitted failures.
    /// </summary>
    public int NumFailures { get; }

    private int _failureCount;

    /// <summary>
    /// Create a new Retry decorator.
    /// </summary>
    /// <param name="name">Name of the decorator.</param>
    /// <param name="child">The child behaviour to decorate.</param>
    /// <param name="numFailures">Maximum number of permitted failures.</param>
    public Retry(string name, Behaviour child, int numFailures) : base(name, child)
    {
        NumFailures = numFailures;
    }

    /// <summary>
    /// Reset the currently registered number of attempts.
    /// </summary>
    protected override void Initialize()
    {
        _failureCount = 0;
    }

    /// <summary>
    /// Retry until failure count is reached.
    /// </summary>
    protected async override Task<Status> Update()
    {
        if (Decorated.Status == Status.Failure)
        {
            _failureCount++;
            if (_failureCount < NumFailures)
            {
                FeedbackMessage = $"attempt failed [status: {_failureCount} failure from {NumFailures}]";
                return await Task.FromResult(Status.Running);
            }
            FeedbackMessage = $"final failure [status: {_failureCount} failure from {NumFailures}]";
            return await Task.FromResult(Status.Failure);
        }
        if (Decorated.Status == Status.Running)
        {
            FeedbackMessage = $"running [status: {_failureCount} failure from {NumFailures}]";
            return await Task.FromResult(Status.Running);
        }
        // Success
        FeedbackMessage = $"succeeded [status: {_failureCount} failure from {NumFailures}]";
        return await Task.FromResult(Status.Success);
    }
}
