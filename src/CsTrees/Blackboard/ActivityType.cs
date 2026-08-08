namespace CsTrees.Blackboard;

/// <summary>
/// Represents the type of activity performed on a blackboard key.
/// </summary>
public enum ActivityType
{
    /// <summary>Read the value of a key.</summary>
    Read,

    /// <summary>Write to an existing key (overwriting previous value).</summary>
    Write,

    /// <summary>Initialize a new key (first write).</summary>
    Initialised,

    /// <summary>Access a complex object (potentially modifying internal properties).</summary>
    Accessed,

    /// <summary>Attempted to access a non-existent key.</summary>
    NoKey,

    /// <summary>Attempted to access a key without proper permission.</summary>
    AccessDenied,

    /// <summary>Attempted to write to an existing key with overwrite disabled.</summary>
    NoOverwrite,

    /// <summary>Key was removed from the blackboard.</summary>
    Unset
}