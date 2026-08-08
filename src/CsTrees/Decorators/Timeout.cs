using System.Diagnostics;

namespace CsTrees.Decorators;

/// <summary>
/// Executes the decorated child with a timeout.
/// <para>
/// If the timeout duration is reached while the child is <see cref="Status.Running"/>,
/// the child is stopped and this decorator returns <see cref="Status.Failure"/>.
/// Otherwise, it reflects the child's status.
/// </para>
/// </summary>
public class Timeout : Decorator
{
    /// <summary>
    /// Timeout duration in seconds.
    /// </summary>
    public double Duration { get; }

    private double _finishTimeTicks;

    /// <summary>
    /// Initialize the Timeout decorator with a timeout duration.
    /// </summary>
    /// <param name="name">The decorator name.</param>
    /// <param name="child">The child behaviour or subtree.</param>
    /// <param name="duration">Timeout length in seconds.</param>
    public Timeout(string name, Behaviour child, double duration = 5.0) : base(name, child)
    {
        Duration = duration;
    }

    /// <summary>
    /// Reset the feedback message and finish time on behaviour entry.
    /// </summary>
    protected override void Initialize()
    {
        _finishTimeTicks = Stopwatch.GetTimestamp() + Duration * Stopwatch.Frequency;
        FeedbackMessage = string.Empty;
    }

    /// <summary>
    /// Fail on timeout, or block / reflect the child's result accordingly.
    /// <para>
    /// Terminate the child and return <see cref="Status.Failure"/> if the timeout is exceeded.
    /// </para>
    /// </summary>
    /// <returns>The behaviour's new status.</returns>
    protected async override Task<Status> Update()
    {
        var now = Stopwatch.GetTimestamp();
        if (Decorated.Status == Status.Running && now > _finishTimeTicks)
        {
            FeedbackMessage = "timed out";
            // Invalidate the decorated child (cancel it)
            Decorated.Stop(Status.Invalid);
            return await Task.FromResult(Status.Failure);
        }
        if (Decorated.Status == Status.Running)
        {
            var remaining = (_finishTimeTicks - now) / Stopwatch.Frequency;
            FeedbackMessage = $"time still ticking ... [remaining: {remaining:F1}s]";
        }
        else
        {
            FeedbackMessage = "child finished before timeout triggered";
        }
        return await Task.FromResult(Decorated.Status);
    }
}
