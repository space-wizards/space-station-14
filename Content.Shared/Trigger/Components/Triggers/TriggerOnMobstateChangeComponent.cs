using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when this entity's mob state changes.
/// The user is the entity that caused the state change or the owner depending on the settings.
/// If added to an implant it will trigger when the implanted entity's mob state changes.
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

    /// <summary>
    /// If false, the trigger user will be the entity that caused the mobstate to change.
    /// If true, the trigger user will the entity that changed its mob state.
    /// </summary>
    /// <summary>
    /// Set this to true for implants that apply an effect on the implanted entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TargetMobstateEntity = true;
}
