using Content.Shared.Trigger.Systems;

namespace Content.Shared.Trigger.Components.Conditions;

/// <summary>
/// Base class for components that add a condition to triggers.
/// </summary>
public abstract partial class BaseTriggerConditionComponent : Component
{
    /// <summary>
    /// The keys that are checked for the condition.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> Keys = new() { TriggerSystem.DefaultTriggerKey };

    /// <summary>
    /// The key that will be triggered if this condition successfully cancels a trigger.
    /// null will result in no trigger occuring.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? CancelKeyOut;

    /// <summary>
    /// If true, this condition will evaluate to the opposite result.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Inverted;
}
