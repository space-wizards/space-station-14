using Content.Server.DeviceLinking.Components;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.DeviceLinking;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Monitor.Components;

[RegisterComponent]
public sealed partial class AirAlarmComponent : Component
{
    [ViewVariables] public AirAlarmMode CurrentMode { get; set; } = AirAlarmMode.Filtering;
    [ViewVariables] public bool AutoMode { get; set; } = true;

    // Remember to null this afterwards.
    [ViewVariables] public IAirAlarmModeUpdate? CurrentModeUpdater { get; set; }

    [ViewVariables] public AirAlarmTab CurrentTab { get; set; }

    public readonly HashSet<string> KnownDevices = new();
    public readonly Dictionary<string, GasVentPumpData> VentData = new();
    public readonly Dictionary<string, GasVentScrubberData> ScrubberData = new();
    public readonly Dictionary<string, AtmosSensorData> SensorData = new();

    public bool CanSync = true;

    /// <summary>
    /// Previous alarm state for use with output ports.
    /// </summary>
    [DataField("state")]
    public AtmosAlarmType State = AtmosAlarmType.Normal;

    /// <summary>
    /// The port that gets set to high while the alarm is in the danger state, and low when not.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> DangerPort = "AirDanger";

    /// <summary>
    /// The port that gets set to high while the alarm is in the warning state, and low when not.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> WarningPort = "AirWarning";

    /// <summary>
    /// The port that gets set to high while the alarm is in the normal state, and low when not.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> NormalPort = "AirNormal";
}
