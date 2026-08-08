namespace CsTrees;

/// <summary>
/// Represents the status of a behaviour node.
/// </summary>
public enum Status
{
    /// <summary>Behaviour is uninitialised and/or in an inactive state.</summary>
    Invalid,
    /// <summary>Behaviour check has passed, or execution finished with a successful result.</summary>
    Success,
    /// <summary>Behaviour check has failed, or execution finished with a failed result.</summary>
    Failure,
    /// <summary>Behaviour is in the middle of executing some action, result still pending.</summary>
    Running
}
