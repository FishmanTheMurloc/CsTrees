namespace CsTrees.Composites;

/// <summary>
/// Configurable policies for <see cref="Parallel"/> behaviours.
/// </summary>
public abstract class ParallelPolicy
{
    /// <summary>
    /// Whether to stop ticking children that have already returned
    /// <see cref="Status.Success"/> until the policy criteria is met.
    /// </summary>
    public bool Synchronise { get; protected set; }

    /// <summary>
    /// Configure the policy to be synchronised or otherwise.
    /// </summary>
    /// <param name="synchronise">If <c>true</c>, stop ticking children with
    /// <see cref="Status.Success"/> until the policy criteria is met.</param>
    protected ParallelPolicy(bool synchronise = false)
    {
        Synchronise = synchronise;
    }

    /// <summary>
    /// Success depends on all children succeeding.
    /// Returns <see cref="Status.Success"/> only when every child returns
    /// <see cref="Status.Success"/>.
    /// </summary>
    public class SuccessOnAll : ParallelPolicy
    {
        /// <summary>
        /// Create a SuccessOnAll policy.
        /// </summary>
        /// <param name="synchronise">If <c>true</c>, stop ticking children that have already
        /// returned <see cref="Status.Success"/> until all children succeed.</param>
        public SuccessOnAll(bool synchronise = true) : base(synchronise) { }
    }

    /// <summary>
    /// Success depends on just one child.
    /// Returns <see cref="Status.Success"/> when at least one child returns
    /// <see cref="Status.Success"/>.
    /// </summary>
    public class SuccessOnOne : ParallelPolicy
    {
        /// <summary>
        /// Create a SuccessOnOne policy. No configuration necessary.
        /// </summary>
        public SuccessOnOne() : base(synchronise: false) { }
    }

    /// <summary>
    /// Success depends on an explicitly selected set of children.
    /// Returns <see cref="Status.Success"/> when each child in the specified
    /// list returns <see cref="Status.Success"/>.
    /// </summary>
    public class SuccessOnSelected : ParallelPolicy
    {
        /// <summary>
        /// The set of children that must succeed for the parallel to succeed.
        /// </summary>
        public List<Behaviour> Children { get; }

        /// <summary>
        /// Create a SuccessOnSelected policy.
        /// </summary>
        /// <param name="children">List of children that must succeed for the parallel to succeed.</param>
        /// <param name="synchronise">If <c>true</c>, stop ticking children that have already
        /// returned <see cref="Status.Success"/> until the selected children all succeed.</param>
        public SuccessOnSelected(List<Behaviour> children, bool synchronise = true) : base(synchronise)
        {
            Children = children;
        }
    }
}

/// <summary>
/// Parallels enable conceptual concurrency — every child is ticked every time
/// the parallel is itself ticked. The parallelism is not true multithreading;
/// children are ticked sequentially but from the tree's perspective, all children
/// are considered to have been ticked at once.
/// <para>
/// Rules:
/// <list type="bullet">
///   <item>Returns <see cref="Status.Failure"/> if any child returns <see cref="Status.Failure"/>.</item>
///   <item>With <see cref="ParallelPolicy.SuccessOnAll"/>: returns <see cref="Status.Success"/>
///     only when ALL children succeed.</item>
///   <item>With <see cref="ParallelPolicy.SuccessOnOne"/>: returns <see cref="Status.Success"/>
///     when at least one child succeeds.</item>
///   <item>With <see cref="ParallelPolicy.SuccessOnSelected"/>: returns <see cref="Status.Success"/>
///     when a specified subset of children all succeed.</item>
/// </list>
/// </para>
/// </summary>
public class Parallel : Composite
{
    /// <summary>
    /// The policy that determines when this parallel succeeds.
    /// </summary>
    public ParallelPolicy Policy { get; }

    /// <summary>
    /// Create a new Parallel behaviour.
    /// </summary>
    /// <param name="name">Name of the composite behaviour.</param>
    /// <param name="policy">Policy for deciding success or otherwise.</param>
    /// <param name="children">List of children to add.</param>
    public Parallel(string name, ParallelPolicy policy, IEnumerable<Behaviour>? children = null)
        : base(name, children)
    {
        Policy = policy;
    }

    /// <summary>
    /// Detect before ticking whether the policy configuration is invalid.
    /// </summary>
    public override void Setup()
    {
        ValidatePolicyConfiguration();
    }

    /// <summary>
    /// Tick all children and determine the result based on the configured policy.
    /// </summary>
    public override async IAsyncEnumerable<Behaviour> Tick()
    {
        ValidatePolicyConfiguration();

        // Reset
        if (Status != Status.Running)
        {
            foreach (var child in Children)
            {
                if (child.Status != Status.Invalid)
                    child.Stop(Status.Invalid);
            }
            CurrentChild = null;
            Initialize();
        }

        // Nothing to do
        if (Children.Count == 0)
        {
            CurrentChild = null;
            Stop(Status.Success);
            yield return this;
            yield break;
        }

        // Process all children
        foreach (var child in Children)
        {
            if (Policy.Synchronise && child.Status == Status.Success)
                continue;
            await foreach (var node in child.Tick())
            {
                yield return node;
            }
        }

        // Determine new status
        var newStatus = Status.Running;
        CurrentChild = Children[Children.Count - 1];

        var failedChild = Children.FirstOrDefault(c => c.Status == Status.Failure);
        if (failedChild is not null)
        {
            CurrentChild = failedChild;
            newStatus = Status.Failure;
        }
        else
        {
            switch (Policy)
            {
                case ParallelPolicy.SuccessOnAll:
                    if (Children.All(c => c.Status == Status.Success))
                    {
                        newStatus = Status.Success;
                        CurrentChild = Children[Children.Count - 1];
                    }
                    break;

                case ParallelPolicy.SuccessOnOne:
                    for (int i = Children.Count - 1; i >= 0; i--)
                    {
                        if (Children[i].Status == Status.Success)
                        {
                            newStatus = Status.Success;
                            CurrentChild = Children[i];
                            break;
                        }
                    }
                    break;

                case ParallelPolicy.SuccessOnSelected selectedPolicy:
                    if (selectedPolicy.Children.All(c => c.Status == Status.Success))
                    {
                        newStatus = Status.Success;
                        CurrentChild = selectedPolicy.Children[selectedPolicy.Children.Count - 1];
                    }
                    break;

                default:
                    throw new InvalidOperationException(
                        $"This parallel has been configured with an unrecognised policy [{Policy.GetType()}]");
            }
        }

        // Stop running children if the parallel has reached a final status
        if (newStatus != Status.Running)
            Stop(newStatus);
        Status = newStatus;
        yield return this;
    }

    /// <summary>
    /// Stop the parallel, ensuring any running children are also stopped.
    /// </summary>
    public override void Stop(Status newStatus)
    {
        // Clean up dangling (running) children
        foreach (var child in Children)
        {
            if (child.Status == Status.Running)
                child.Stop(Status.Invalid);
        }
        base.Stop(newStatus);
    }

    private void ValidatePolicyConfiguration()
    {
        if (Policy is ParallelPolicy.SuccessOnSelected selectedPolicy)
        {
            if (selectedPolicy.Children.Count == 0)
                throw new InvalidOperationException(
                    $"Policy SuccessOnSelected requires a non-empty selection of children [{Name}]");

            var missingNames = selectedPolicy.Children
                .Where(c => !Children.Contains(c))
                .Select(c => c.Name)
                .ToList();

            if (missingNames.Count > 0)
                throw new InvalidOperationException(
                    $"Policy SuccessOnSelected has selected behaviours that are not children " +
                    $"of this parallel [{string.Join(", ", missingNames)}][{Name}]");
        }
    }
}
