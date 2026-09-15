using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.GameStates;

namespace Content.Shared.Mobs.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(MobStateSystem))]
public sealed partial class ActionRequireMobStateComponent : Component
{
    /// <summary>
    /// The required MobState for the action.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public HashSet<MobState> States = new();

    /// <summary>
    /// The type of Popup to be used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? FailReason = "mob-state-action-requires-state";

    /// <summary>
    /// The type of the popup the fail reason should show as.
    /// </summary>
    [DataField, AutoNetworkedField]
    public PopupType FailReasonPopupType = PopupType.SmallCaution;
}
