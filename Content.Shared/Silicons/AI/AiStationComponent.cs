using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.AI;

/// <summary>
/// This is used for the core body of a borg. This manages a borg's
/// "brain", legs, modules, and battery. Essentially the master component
/// for borg logic.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedStationAISystem)), AutoGenerateComponentState]
public sealed partial class StationAIComponent : Component
{
    /// <summary>
    /// Whether or not the AI is activated
    /// </summary>
    [DataField("activated"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Activated;
}


[RegisterComponent, Access(typeof(SharedStationAISystem))]
public sealed partial class ActionStationAIComponent : Component
{
    /// <summary>
    /// The sidebar action that toggles the IA view.
    /// </summary>
    [DataField]
    public EntProtoId CameraMonitorAIAction = "ActionAICameraViewer";
    /// <summary>
    /// The action for toggling view.
    /// </summary>
    [DataField]
    public EntityUid? CameraMonitorAIActionEntity;
}

public sealed partial class ToggleAICameraMonitorEvent : InstantActionEvent
{

}

