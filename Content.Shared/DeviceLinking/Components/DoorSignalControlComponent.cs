using Content.Shared.DeviceLinking.Systems;
using Content.Shared.Doors.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Components;

/// <summary>
/// Component that allows to control a <see cref="DoorComponent"/> using Device linking.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(DoorSignalControlSystem))]
public sealed partial class DoorSignalControlComponent : Component
{
    [DataField]
    public ProtoId<SinkPortPrototype> OpenPort = "Open";

    [DataField]
    public ProtoId<SinkPortPrototype> ClosePort = "Close";

    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    [DataField("boltPort")]
    public ProtoId<SinkPortPrototype> InBolt = "DoorBolt";

    [DataField("onOpenPort")]
    public ProtoId<SourcePortPrototype> OutOpen = "DoorStatus";

    [DataField("onBoltPort")]
    public ProtoId<SourcePortPrototype> OutBolt = "DoorBoltStatus";
}
