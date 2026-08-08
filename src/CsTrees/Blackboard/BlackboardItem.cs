namespace CsTrees.Blackboard;

/// <summary>
/// Represents a single key-value item in the blackboard for rendering.
/// </summary>
public sealed class BlackboardItem
{
    /// <summary>The name of the blackboard key.</summary>
    public string Key { get; }

    /// <summary>The value associated with the key. Null if <see cref="HasValue"/> is false.</summary>
    public object? Value { get; }

    /// <summary>Whether the key has been set in the blackboard.</summary>
    public bool HasValue { get; }

    /// <summary>
    /// Creates a new BlackboardItem instance.
    /// </summary>
    /// <param name="key">The name of the blackboard key.</param>
    /// <param name="value">The value associated with the key.</param>
    /// <param name="hasValue">Whether the key has been set.</param>
    public BlackboardItem(string key, object? value, bool hasValue)
    {
        Key = key;
        Value = value;
        HasValue = hasValue;
    }
}