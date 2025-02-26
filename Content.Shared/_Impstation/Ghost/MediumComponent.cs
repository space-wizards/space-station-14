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

    //Time in seconds passed since medium vision activated
    [DataField, AutoNetworkedField]
    public float CurrentMediumTime = 0;

    //Time after how many seconds the medium effect stops
    //Im just gonna put it here as a constant instead of making a whole prototype to set it from the yaml
    //Because Im not expected for other reagents to reuse that effect and even less so with a different time limit
    [DataField, AutoNetworkedField]
    public float MediumTime = 300; // 5 minutes
}

public sealed partial class ToggleGhostsMediumActionEvent : InstantActionEvent { }
