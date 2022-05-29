using Content.Shared.Atmos.Monitor.Components;
using Robust.Shared.Network;

namespace Content.Server.Atmos.Monitor.Components
{
    [RegisterComponent]
    public sealed class AirAlarmComponent : Component
    {
        [ViewVariables] public AirAlarmMode CurrentMode { get; set; } = AirAlarmMode.Filtering;

        // Remember to null this afterwards.
        [ViewVariables] public IAirAlarmModeUpdate? CurrentModeUpdater { get; set; }

        public Dictionary<string, IAtmosDeviceData> DeviceData = new();

        public HashSet<NetUserId> ActivePlayers = new();

        public bool CanSync = true;
    }
}
