using Content.Shared.Trigger.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Conditions;

/// <summary>
/// Base class for components that add a condition to triggers.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public abstract partial class BaseTriggerConditionComponent : Component
{
    /// <summary>
    /// The keys that are checked for the condition.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> Keys = new() { TriggerSystem.DefaultTriggerKey };
}
