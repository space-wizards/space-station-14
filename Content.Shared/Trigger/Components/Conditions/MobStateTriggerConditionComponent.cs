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
    /// If the user is in one of these mob states, the trigger won't cancel.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<MobState> MobStates = new ();
}
