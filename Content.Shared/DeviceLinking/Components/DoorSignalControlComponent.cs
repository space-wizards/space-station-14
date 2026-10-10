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
    /// <summary>
    /// Input port that opens the door.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> OpenPort = "Open";

    /// <summary>
    /// Input port that closes the door.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> ClosePort = "Close";

    /// <summary>
    /// Input port that toggles the door's open state.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    /// <summary>
    /// Input port that controls the door bolt.
    /// </summary>
    [DataField("boltPort")]
    public ProtoId<SinkPortPrototype> InBolt = "DoorBolt";

    /// <summary>
    /// Output port that reports the door's open state.
    /// </summary>
    [DataField("onOpenPort")]
    public ProtoId<SourcePortPrototype> OutOpen = "DoorStatus";

    /// <summary>
    /// Output port that reports the door bolt state.
    /// </summary>
    [DataField("onBoltPort")]
    public ProtoId<SourcePortPrototype> OutBolt = "DoorBoltStatus";
}
