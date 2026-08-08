using CsTrees;

namespace CsTrees.Blackboard;

/// <summary>
/// Blackboard is a key-value store shared across behaviour tree nodes.
/// </summary>
public sealed class Blackboard
{
    /// <summary>
    /// Separator used for namespacing keys (e.g., "/namespace/key").
    /// </summary>
    public const char Separator = '/';

    private readonly Dictionary<string, object> _storage = new();
    private readonly HashSet<BehaviourKeyAccess> _behaviourKeyAccesses = new();

    /// <summary>
    /// Activity stream for recording blackboard operations. Null if not enabled.
    /// </summary>
    public ActivityStream? ActivityStream { get; private set; }

    /// <summary>
    /// All access grants registered on this blackboard.
    /// </summary>
    public IReadOnlyCollection<BehaviourKeyAccess> BehaviourKeyAccesses => _behaviourKeyAccesses;

    /// <summary>
    /// Enable activity stream recording.
    /// </summary>
    /// <param name="maximumSize">Maximum number of items to store. Default is 500.</param>
    public void EnableActivityStream(int maximumSize = 500)
        => ActivityStream = new ActivityStream(maximumSize);

    /// <summary>
    /// Disable activity stream recording.
    /// </summary>
    public void DisableActivityStream()
        => ActivityStream = null;

    /// <summary>
    /// Grant read access to a key for a behaviour.
    /// </summary>
    public BehaviourKeyAccess<T> GrantRead<T>(Behaviour behaviour, string key)
    {
        ValidateKey<T>(key);
        var access = new BehaviourKeyAccess<T>(this, key, behaviour, Access.Read);
        _behaviourKeyAccesses.Add(access);
        return access;
    }

    /// <summary>
    /// Grant write access (including read) to a key for a behaviour.
    /// Multiple writers are allowed for the same key.
    /// </summary>
    public BehaviourKeyAccess<T> GrantWrite<T>(Behaviour behaviour, string key)
    {
        ValidateKey<T>(key);

        // Check for exclusive write conflict
        if (_behaviourKeyAccesses.Any(a => a.Key == key && a.Access == Access.ExclusiveWrite))
            throw new InvalidOperationException(
                $"Key '{key}' has an exclusive writer");

        var access = new BehaviourKeyAccess<T>(this, key, behaviour, Access.Write);
        _behaviourKeyAccesses.Add(access);
        return access;
    }

    /// <summary>
    /// Grant exclusive write access to a key for a behaviour.
    /// Only one exclusive writer is allowed, and no other writers are permitted.
    /// </summary>
    public BehaviourKeyAccess<T> GrantExclusiveWrite<T>(Behaviour behaviour, string key)
    {
        ValidateKey<T>(key);

        // Check for existing exclusive writer
        if (_behaviourKeyAccesses.Any(a => a.Key == key && a.Access == Access.ExclusiveWrite))
            throw new InvalidOperationException(
                $"Key '{key}' already has an exclusive writer");

        // Check for existing writers
        if (_behaviourKeyAccesses.Any(a => a.Key == key && a.Access == Access.Write))
            throw new InvalidOperationException(
                $"Key '{key}' already has writer(s), cannot grant exclusive write");

        var access = new BehaviourKeyAccess<T>(this, key, behaviour, Access.ExclusiveWrite);
        _behaviourKeyAccesses.Add(access);
        return access;
    }

    private void ValidateKey<T>(string key)
    {
        var existing = _behaviourKeyAccesses.FirstOrDefault(a => a.Key == key);
        if (existing is not null && existing.ValueType != typeof(T))
            throw new InvalidOperationException(
                $"Key '{key}' already registered as {existing.ValueType.Name}, cannot use as {typeof(T).Name}");
    }

    internal void Set<T>(BehaviourKeyAccess<T> access, T value)
    {
        // Check write permission
        if (access.Access == Access.Read)
        {
            ActivityStream?.Push(new ActivityItem(
                access.Key,
                access.Behaviour.Name,
                access.Behaviour.Id,
                ActivityType.AccessDenied));

            throw new UnauthorizedAccessException(
                $"Behaviour '{access.Behaviour.Name}' has read-only access to key '{access.Key}'");
        }

        var previousValue = _storage.TryGetValue(access.Key, out var v) ? v : null;
        var activityType = previousValue is null ? ActivityType.Initialised : ActivityType.Write;

        _storage[access.Key] = value!;

        ActivityStream?.Push(new ActivityItem(
            access.Key,
            access.Behaviour.Name,
            access.Behaviour.Id,
            activityType,
            previousValue,
            value));
    }

    internal bool Set<T>(BehaviourKeyAccess<T> access, T value, bool overwrite)
    {
        // Check write permission
        if (access.Access == Access.Read)
        {
            ActivityStream?.Push(new ActivityItem(
                access.Key,
                access.Behaviour.Name,
                access.Behaviour.Id,
                ActivityType.AccessDenied));

            throw new UnauthorizedAccessException(
                $"Behaviour '{access.Behaviour.Name}' has read-only access to key '{access.Key}'");
        }

        // Key already exists
        if (_storage.ContainsKey(access.Key))
        {
            if (!overwrite)
            {
                // NO_OVERWRITE
                ActivityStream?.Push(new ActivityItem(
                    access.Key,
                    access.Behaviour.Name,
                    access.Behaviour.Id,
                    ActivityType.NoOverwrite,
                    currentValue: _storage[access.Key]));

                return false;
            }

            // WRITE (overwrite existing)
            var previousValue = _storage[access.Key];
            _storage[access.Key] = value!;
            ActivityStream?.Push(new ActivityItem(
                access.Key,
                access.Behaviour.Name,
                access.Behaviour.Id,
                ActivityType.Write,
                previousValue,
                value));

            return true;
        }

        // Key does not exist -> INITIALISED
        _storage[access.Key] = value!;
        ActivityStream?.Push(new ActivityItem(
            access.Key,
            access.Behaviour.Name,
            access.Behaviour.Id,
            ActivityType.Initialised,
            currentValue: value));

        return true;
    }

    internal void Unset<T>(BehaviourKeyAccess<T> access)
    {
        // Check write permission
        if (access.Access == Access.Read)
        {
            ActivityStream?.Push(new ActivityItem(
                access.Key,
                access.Behaviour.Name,
                access.Behaviour.Id,
                ActivityType.AccessDenied));

            throw new UnauthorizedAccessException(
                $"Behaviour '{access.Behaviour.Name}' has read-only access to key '{access.Key}'");
        }

        var previousValue = _storage.TryGetValue(access.Key, out var v) ? v : null;
        _storage.Remove(access.Key);

        ActivityStream?.Push(new ActivityItem(
            access.Key,
            access.Behaviour.Name,
            access.Behaviour.Id,
            ActivityType.Unset,
            previousValue: previousValue));
    }

    internal T Get<T>(BehaviourKeyAccess<T> access)
    {
        if (!TryGet(access, out var t))
        {
            throw new Exception($"{access.Key} missing or type mismatch");
        }

        var activityType = IsPrimitive(t) ? ActivityType.Read : ActivityType.Accessed;
        ActivityStream?.Push(new ActivityItem(
            access.Key,
            access.Behaviour.Name,
            access.Behaviour.Id,
            activityType,
            currentValue: t));

        return t;
    }

    internal bool TryGet<T>(BehaviourKeyAccess<T> access, out T value)
    {
        if (_storage.TryGetValue(access.Key, out var v) && v is T t)
        {
            value = t;
            return true;
        }

        value = default!;
        ActivityStream?.Push(new ActivityItem(
            access.Key,
            access.Behaviour.Name,
            access.Behaviour.Id,
            ActivityType.NoKey));

        return false;
    }

    internal bool Exists<T>(BehaviourKeyAccess<T> access)
        => _storage.ContainsKey(access.Key);

    private static bool IsPrimitive(object? value)
        => value is null
        || value.GetType().IsPrimitive
        || value is string
        || value.GetType().IsEnum;

    /// <summary>
    /// Clear all key-value pairs from the blackboard.
    /// </summary>
    public void Clear() => _storage.Clear();

    /// <summary>
    /// Get all key-value items from the blackboard.
    /// Callers can filter/transform the result as needed before rendering.
    /// </summary>
    /// <returns>An enumerable of <see cref="BlackboardItem"/> representing all registered keys.</returns>
    public IEnumerable<BlackboardItem> GetItems()
    {
        // Return all registered keys (from BehaviourKeyAccesses), including those not yet set
        var registeredKeys = _behaviourKeyAccesses.Select(a => a.Key).Distinct();
        foreach (var key in registeredKeys)
        {
            var hasValue = _storage.TryGetValue(key, out var value);
            yield return new BlackboardItem(key, value, hasValue);
        }
    }
}