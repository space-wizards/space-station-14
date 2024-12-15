using Content.Shared.Actions;
using Content.Shared.Ghost;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.Ghost;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedGhostSystem))]
[AutoGenerateComponentState(true)]
public sealed partial class MediumComponent : Component
{
    [DataField]
    public EntProtoId ToggleGhostsMediumAction = "ActionToggleGhostsMedium";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleGhostsMediumActionEntity;
}

public sealed partial class ToggleGhostsMediumActionEvent : InstantActionEvent { }
