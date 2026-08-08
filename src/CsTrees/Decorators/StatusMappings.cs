namespace CsTrees.Decorators;

/// <summary>
/// Reflect <see cref="Status.Running"/> as <see cref="Status.Failure"/>.
/// </summary>
public class RunningIsFailure : Decorator
{
    /// <summary>
    /// Initialize the RunningIsFailure decorator.
    /// </summary>
    /// <param name="name">The decorator name.</param>
    /// <param name="child">The child behaviour or subtree.</param>
    public RunningIsFailure(string name, Behaviour child) : base(name, child) { }

    /// <summary>
    /// Reflect <see cref="Status.Running"/> as <see cref="Status.Failure"/>.
    /// </summary>
    /// <returns>The behaviour's new status.</returns>
    protected async override Task<Status> Update()
    {
        if (Decorated.Status == Status.Running)
        {
            FeedbackMessage = "running is failure" +
                (!string.IsNullOrEmpty(Decorated.FeedbackMessage) ? $" [{Decorated.FeedbackMessage}]" : "");
            return await Task.FromResult(Status.Failure);
        }
        FeedbackMessage = Decorated.FeedbackMessage;
        return await Task.FromResult(Decorated.Status);
    }
}

/// <summary>
/// Reflect <see cref="Status.Running"/> as <see cref="Status.Success"/>.
/// </summary>
public class RunningIsSuccess : Decorator
{
    /// <summary>
    /// Initialize the RunningIsSuccess decorator.
    /// </summary>
    /// <param name="name">The decorator name.</param>
    /// <param name="child">The child behaviour or subtree.</param>
    public RunningIsSuccess(string name, Behaviour child) : base(name, child) { }

    /// <summary>
    /// Reflect <see cref="Status.Running"/> as <see cref="Status.Success"/>.
    /// </summary>
    /// <returns>The behaviour's new status.</returns>
    protected async override Task<Status> Update()
    {
        if (Decorated.Status == Status.Running)
        {
            FeedbackMessage = "running is success" +
                (!string.IsNullOrEmpty(Decorated.FeedbackMessage) ? $" [{Decorated.FeedbackMessage}]" : "");
            return await Task.FromResult(Status.Success);
        }
        FeedbackMessage = Decorated.FeedbackMessage;
        return await Task.FromResult(Decorated.Status);
    }
}

/// <summary>
/// Reflect <see cref="Status.Failure"/> as <see cref="Status.Success"/>.
/// </summary>
public class FailureIsSuccess : Decorator
{
    /// <summary>
    /// Initialize the FailureIsSuccess decorator.
    /// </summary>
    /// <param name="name">The decorator name.</param>
    /// <param name="child">The child behaviour or subtree.</param>
    public FailureIsSuccess(string name, Behaviour child) : base(name, child) { }

    /// <summary>
    /// Reflect <see cref="Status.Failure"/> as <see cref="Status.Success"/>.
    /// </summary>
    /// <returns>The behaviour's new status.</returns>
    protected async override Task<Status> Update()
    {
        if (Decorated.Status == Status.Failure)
        {
            FeedbackMessage = "failure is success" +
                (!string.IsNullOrEmpty(Decorated.FeedbackMessage) ? $" [{Decorated.FeedbackMessage}]" : "");
            return await Task.FromResult(Status.Success);
        }
        FeedbackMessage = Decorated.FeedbackMessage;
        return await Task.FromResult(Decorated.Status);
    }
}

/// <summary>
/// Reflect <see cref="Status.Failure"/> as <see cref="Status.Running"/>.
/// </summary>
public class FailureIsRunning : Decorator
{
    /// <summary>
    /// Initialize the FailureIsRunning decorator.
    /// </summary>
    /// <param name="name">The decorator name.</param>
    /// <param name="child">The child behaviour or subtree.</param>
    public FailureIsRunning(string name, Behaviour child) : base(name, child) { }

    /// <summary>
    /// Reflect <see cref="Status.Failure"/> as <see cref="Status.Running"/>.
    /// </summary>
    /// <returns>The behaviour's new status.</returns>
    protected async override Task<Status> Update()
    {
        if (Decorated.Status == Status.Failure)
        {
            FeedbackMessage = "failure is running" +
                (!string.IsNullOrEmpty(Decorated.FeedbackMessage) ? $" [{Decorated.FeedbackMessage}]" : "");
            return await Task.FromResult(Status.Running);
        }
        FeedbackMessage = Decorated.FeedbackMessage;
        return await Task.FromResult(Decorated.Status);
    }
}

/// <summary>
/// Reflect <see cref="Status.Success"/> as <see cref="Status.Failure"/>.
/// </summary>
public class SuccessIsFailure : Decorator
{
    /// <summary>
    /// Initialize the SuccessIsFailure decorator.
    /// </summary>
    /// <param name="name">The decorator name.</param>
    /// <param name="child">The child behaviour or subtree.</param>
    public SuccessIsFailure(string name, Behaviour child) : base(name, child) { }

    /// <summary>
    /// Reflect <see cref="Status.Success"/> as <see cref="Status.Failure"/>.
    /// </summary>
    /// <returns>The behaviour's new status.</returns>
    protected async override Task<Status> Update()
    {
        if (Decorated.Status == Status.Success)
        {
            FeedbackMessage = "success is failure" +
                (!string.IsNullOrEmpty(Decorated.FeedbackMessage) ? $" [{Decorated.FeedbackMessage}]" : "");
            return await Task.FromResult(Status.Failure);
        }
        FeedbackMessage = Decorated.FeedbackMessage;
        return await Task.FromResult(Decorated.Status);
    }
}

/// <summary>
/// Reflect <see cref="Status.Success"/> as <see cref="Status.Running"/>.
/// </summary>
public class SuccessIsRunning : Decorator
{
    /// <summary>
    /// Initialize the SuccessIsRunning decorator.
    /// </summary>
    /// <param name="name">The decorator name.</param>
    /// <param name="child">The child behaviour or subtree.</param>
    public SuccessIsRunning(string name, Behaviour child) : base(name, child) { }

    /// <summary>
    /// Reflect <see cref="Status.Success"/> as <see cref="Status.Running"/>.
    /// </summary>
    /// <returns>The behaviour's new status.</returns>
    protected async override Task<Status> Update()
    {
        if (Decorated.Status == Status.Success)
        {
            FeedbackMessage = "success is running" +
                (!string.IsNullOrEmpty(Decorated.FeedbackMessage) ? $" [{Decorated.FeedbackMessage}]" : "");
            return await Task.FromResult(Status.Running);
        }
        FeedbackMessage = Decorated.FeedbackMessage;
        return await Task.FromResult(Decorated.Status);
    }
}
