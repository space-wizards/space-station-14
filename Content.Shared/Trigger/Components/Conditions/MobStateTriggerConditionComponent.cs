using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Conditions;

/// <summary>
/// Checks if the user of a trigger satisfies a mob state condition.
/// Cancels the trigger otherwise.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MobStateTriggerConditionComponent : BaseTriggerConditionComponent
{
    /// <summary>
    /// If the user is in this mob state, the trigger won't cancel.
    /// If <see cref="Invert"/> is true, this mob state will cause the trigger to cancel instead.
    /// </summary>
    [DataField, AutoNetworkedField]
    public MobState MobState = MobState.Alive;

    /// <summary>
    /// Inverts the result of the condition.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Invert = false;
}
