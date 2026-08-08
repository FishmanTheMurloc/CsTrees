namespace CsTrees.Blackboard;

/// <summary>
/// Represents a single activity record on the blackboard.
/// </summary>
public sealed class ActivityItem
{
    /// <summary>The name of the blackboard key.</summary>
    public string Key { get; }

    /// <summary>The name of the behaviour that performed the activity.</summary>
    public string BehaviourName { get; }

    /// <summary>The unique identifier of the behaviour.</summary>
    public Guid BehaviourId { get; }

    /// <summary>The type of activity performed.</summary>
    public ActivityType ActivityType { get; }

    /// <summary>The previous value of the key (if applicable).</summary>
    public object? PreviousValue { get; }

    /// <summary>The current value of the key (if applicable).</summary>
    public object? CurrentValue { get; }

    /// <summary>The timestamp when the activity occurred.</summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Creates a new ActivityItem instance.
    /// </summary>
    public ActivityItem(
        string key,
        string behaviourName,
        Guid behaviourId,
        ActivityType activityType,
        object? previousValue = null,
        object? currentValue = null)
    {
        Key = key;
        BehaviourName = behaviourName;
        BehaviourId = behaviourId;
        ActivityType = activityType;
        PreviousValue = previousValue;
        CurrentValue = currentValue;
        Timestamp = DateTime.UtcNow;
    }
}