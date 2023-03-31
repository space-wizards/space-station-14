using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Robust.Shared.Network;

namespace Content.Server.Atmos.Monitor.Components;

[RegisterComponent]
public sealed class AirAlarmComponent : Component
{
    [ViewVariables] public AirAlarmMode CurrentMode { get; set; } = AirAlarmMode.Filtering;

    // Remember to null this afterwards.
    [ViewVariables] public IAirAlarmModeUpdate? CurrentModeUpdater { get; set; }

    [ViewVariables] public AirAlarmTab CurrentTab { get; set; }

    public readonly HashSet<string> KnownDevices = new();
    public readonly Dictionary<string, GasVentPumpData> VentData = new();
    public readonly Dictionary<string, GasVentScrubberData> ScrubberData = new();
    public readonly Dictionary<string, AtmosSensorData> SensorData = new();

    public HashSet<NetUserId> ActivePlayers = new();

    public bool CanSync = true;
}
