using CsTrees;

namespace CsTrees.Blackboard;

/// <summary>
/// Non-generic base class for tracking access grants.
/// </summary>
public abstract class BehaviourKeyAccess
{
    /// <summary>The name of the blackboard key.</summary>
    public abstract string Key { get; }

    /// <summary>The value type of the blackboard key.</summary>
    public abstract Type ValueType { get; }

    /// <summary>The behaviour that has been granted access.</summary>
    public abstract Behaviour Behaviour { get; }

    /// <summary>The access level granted.</summary>
    public abstract Access Access { get; }
}

/// <summary>
/// Generic access handle for a behaviour to read/write a blackboard key.
/// </summary>
public sealed class BehaviourKeyAccess<T> : BehaviourKeyAccess
{
    private readonly Blackboard _bb;

    /// <inheritdoc/>
    public override string Key { get; }

    /// <inheritdoc/>
    public override Type ValueType => typeof(T);

    /// <inheritdoc/>
    public override Behaviour Behaviour { get; }

    /// <inheritdoc/>
    public override Access Access { get; }

    internal BehaviourKeyAccess(Blackboard bb, string key, Behaviour behaviour, Access access)
    {
        _bb = bb;
        Key = key;
        Behaviour = behaviour;
        Access = access;
    }

    /// <summary>Get the value from the blackboard.</summary>
    public T Get() => _bb.Get(this);

    /// <summary>
    /// Try to get the value from the blackboard without throwing.
    /// </summary>
    /// <param name="value">The value if the key exists; otherwise the default value for T.</param>
    /// <returns>True if the key exists and the type matches; false otherwise.</returns>
    public bool TryGet(out T value) => _bb.TryGet(this, out value);

    /// <summary>Check if the key exists in the blackboard.</summary>
    public bool Exists() => _bb.Exists(this);

    /// <summary>Set the value in the blackboard (always overwrites).</summary>
    public void Set(T value) => _bb.Set(this, value);

    /// <summary>Set the value in the blackboard.</summary>
    /// <param name="value">The value to set.</param>
    /// <param name="overwrite">If false, only set if the key does not already exist.</param>
    /// <returns>True if the value was set; false if the key already existed and overwrite was false.</returns>
    public bool Set(T value, bool overwrite) => _bb.Set(this, value, overwrite);

    /// <summary>Remove the key from the blackboard.</summary>
    public void Unset() => _bb.Unset(this);
}