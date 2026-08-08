namespace CsTrees.Behaviours;

/// <summary>
/// Simply periodically rotates its status over all each status.
/// <para>
/// That is, <see cref="Status.Running"/> for N ticks,
/// <see cref="Status.Success"/> for N ticks,
/// <see cref="Status.Failure"/> for N ticks...
/// </para>
/// <para>
/// Note: It does not reset the count when initialising.
/// </para>
/// <para>
/// Corresponds to <c>py_trees.behaviours.Periodic</c>.
/// </para>
/// </summary>
public class Periodic : Behaviour
{
    private int _count;
    private Status _response = Status.Running;

    /// <summary>Period value (in ticks).</summary>
    public int Period { get; }

    /// <summary>
    /// Create a new Periodic behaviour.
    /// </summary>
    /// <param name="name">Name of the behaviour.</param>
    /// <param name="n">Period value in ticks.</param>
    public Periodic(string name, int n) : base(name)
    {
        Period = n;
    }

    /// <summary>
    /// Increment counter and use to decide the current status.
    /// </summary>
    protected async override Task<Status> Update()
    {
        _count++;
        if (_count > Period)
        {
            if (_response == Status.Failure)
            {
                FeedbackMessage = "flip to running";
                _response = Status.Running;
            }
            else if (_response == Status.Running)
            {
                FeedbackMessage = "flip to success";
                _response = Status.Success;
            }
            else
            {
                FeedbackMessage = "flip to failure";
                _response = Status.Failure;
            }
            _count = 0;
        }
        else
        {
            FeedbackMessage = "constant";
        }
        return await Task.FromResult(_response);
    }
}
