namespace CsTrees.Blackboard;

/// <summary>
/// Represents the access level for a blackboard key.
/// </summary>
public enum Access
{
    /// <summary>Read access only.</summary>
    Read,
    /// <summary>Write access (implicitly includes read access).</summary>
    Write,
    /// <summary>Exclusive write access - no other writers permitted.</summary>
    ExclusiveWrite
}