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
    /// null will activate all triggers.
    /// </summary>
    /// <remarks>
    /// If a different condition cancels the trigger but this condition wouldn't,
    /// this key might or might not occur due to event ordering.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public string? CancelKeyOut = TriggerSystem.CancelledTriggerKey;
}
