using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Actions;

namespace Content.Shared.PAI;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CommandPAIComponent : Component
{

    [DataField]
    public ProtoId<EntityPrototype> CameraMonitorActionId = "ActionPAIShowCameraMonitor";

    [DataField, AutoNetworkedField]
    public EntityUid? CameraMonitorAction;


    [DataField]
    public ProtoId<EntityPrototype> CrewMonitorActionId = "ActionPAIShowCrewMonitoring";

    [DataField, AutoNetworkedField]
    public EntityUid? CrewMonitorAction;

}
