using System.Diagnostics.CodeAnalysis;

namespace Content.Server.AI;

[DataDefinition]
public sealed class NPCBlackboard : Dictionary<string, object>
{
    private static readonly Dictionary<string, object> BlackboardDefaults = new()
    {
        {"MaximumIdleTime", 7f},
        {"MinimumIdleTime", 2f},
        {"VisionRadius", 7f},
    };

    public NPCBlackboard ShallowClone()
    {
        var dict = new NPCBlackboard();
        foreach (var item in this)
        {
            dict.SetValue(item.Key, item.Value);
        }
        return dict;
    }

    /// <summary>
    /// Get the blackboard data for a particular key.
    /// </summary>
    public T GetValue<T>(string key)
    {
        return (T) this[key];
    }

    /// <summary>
    /// Tries to get the blackboard data for a particular key. Returns default if not found
    /// </summary>
    public T? GetValueOrDefault<T>(string key)
    {
        if (TryGetValue(key, out var value))
        {
            return (T) value;
        }

        if (BlackboardDefaults.TryGetValue(key, out value))
        {
            return (T) value;
        }

        return default;
    }

    /// <summary>
    /// Tries to get the blackboard data for a particular key.
    /// </summary>
    public bool TryGetValue<T>(string key, [NotNullWhen(true)] out T? value)
    {
        if (TryGetValue(key, out var data))
        {
            value = (T) data;
            return true;
        }

        value = default;
        return false;
    }

    public void SetValue(string key, object value)
    {
        this[key] = value;
    }

    /*
    * Constants to make development easier
    */

    public const string Owner = "Owner";
    public const string MovementTarget = "MovementTarget";
    public const string VisionRadius = "VisionRadius";
}
