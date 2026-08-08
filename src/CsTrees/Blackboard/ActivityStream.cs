namespace CsTrees.Blackboard;

/// <summary>
/// A circular buffer for recording blackboard activity history.
/// </summary>
public sealed class ActivityStream
{
    private readonly List<ActivityItem> _data = new();

    /// <summary>The maximum number of items to store. Older items are removed when exceeded.</summary>
    public int MaximumSize { get; }

    /// <summary>The recorded activity items (oldest first).</summary>
    public IReadOnlyList<ActivityItem> Data => _data;

    /// <summary>
    /// Creates a new ActivityStream with the specified maximum size.
    /// </summary>
    /// <param name="maximumSize">Maximum number of items to store. Default is 500.</param>
    public ActivityStream(int maximumSize = 500)
    {
        MaximumSize = maximumSize;
    }

    /// <summary>
    /// Push a new activity item to the stream.
    /// If the stream exceeds MaximumSize, the oldest item is removed.
    /// </summary>
    public void Push(ActivityItem item)
    {
        if (_data.Count >= MaximumSize)
            _data.RemoveAt(0);
        _data.Add(item);
    }

    /// <summary>
    /// Clear all activity items from the stream.
    /// </summary>
    public void Clear() => _data.Clear();
}