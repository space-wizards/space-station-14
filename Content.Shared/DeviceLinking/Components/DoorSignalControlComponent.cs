using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceLinking.Components;

[RegisterComponent, NetworkedComponent]
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
}
