using System.Linq;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.SuitSensors;
using Content.Server.UserInterface;
using Content.Shared.Medical.CrewMonitoring;
using Robust.Shared.Map;
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Timing;
using Content.Server.PowerCell;

namespace Content.Server.Medical.CrewMonitoring
{
    public sealed class CrewMonitoringConsoleSystem : EntitySystem
    {
        [Dependency] private readonly SuitSensorSystem _sensors = default!;
        [Dependency] private readonly SharedTransformSystem _xform = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly PowerCellSystem _cell = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<CrewMonitoringConsoleComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
            SubscribeLocalEvent<CrewMonitoringConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);
        }

        private void OnRemove(EntityUid uid, CrewMonitoringConsoleComponent component, ComponentRemove args)
        {
            component.ConnectedSensors.Clear();
        }

        private void OnPacketReceived(EntityUid uid, CrewMonitoringConsoleComponent component, DeviceNetworkPacketEvent args)
        {
            var payload = args.Data;
            // check command
            if (!payload.TryGetValue(DeviceNetworkConstants.Command, out string? command))
                return;
            if (command != DeviceNetworkConstants.CmdUpdatedState)
                return;
            if (!payload.TryGetValue(SuitSensorConstants.NET_STATUS_COLLECTION, out Dictionary<string, SuitSensorStatus>? sensorStatus))
                return;

            component.ConnectedSensors = sensorStatus;
            UpdateUserInterface(uid, component);
        }

        private void OnUIOpened(EntityUid uid, CrewMonitoringConsoleComponent component, BoundUIOpenedEvent args)
        {
            if (!_cell.TryUseActivatableCharge(uid))
                return;

            UpdateUserInterface(uid, component);
        }

        private void UpdateUserInterface(EntityUid uid, CrewMonitoringConsoleComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var ui = component.Owner.GetUIOrNull(CrewMonitoringUIKey.Key);
            if (ui == null)
                return;

            // update all sensors info
            var allSensors = component.ConnectedSensors.Values.ToList();
            var uiState = new CrewMonitoringState(allSensors, component.Snap, component.Precision);
            ui.SetState(uiState);
        }
    }
}
