using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when this entity's mob state changes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnMobstateChangeComponent : BaseTriggerOnXComponent
{
    /// <summary>
    /// What states should trigger this?
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<MobState> MobState = new();

    /// <summary>
    /// If true, prevents suicide attempts for the trigger to prevent cheese.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool PreventSuicide = false;
}
