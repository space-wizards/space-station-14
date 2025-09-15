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
}
