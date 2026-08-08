namespace CsTrees.Decorators;

/// <summary>
/// A decorator that inverts the result of its child.
/// <para>
/// Flips <see cref="Status.Success"/> ↔ <see cref="Status.Failure"/>.
/// <see cref="Status.Running"/> is passed through unchanged.
/// </para>
/// </summary>
public class Inverter : Decorator
{
    /// <summary>
    /// Create a new Inverter decorator.
    /// </summary>
    /// <param name="name">Name of the decorator.</param>
    /// <param name="child">The child behaviour to decorate.</param>
    public Inverter(string name, Behaviour child) : base(name, child) { }

    /// <summary>
    /// Flip <see cref="Status.Success"/> and <see cref="Status.Failure"/>.
    /// </summary>
    protected async override Task<Status> Update()
    {
        if (Decorated.Status == Status.Success)
        {
            FeedbackMessage = "success -> failure";
            return await Task.FromResult(Status.Failure);
        }
        if (Decorated.Status == Status.Failure)
        {
            FeedbackMessage = "failure -> success";
            return await Task.FromResult(Status.Success);
        }
        FeedbackMessage = Decorated.FeedbackMessage;
        return await Task.FromResult(Decorated.Status);
    }
}
