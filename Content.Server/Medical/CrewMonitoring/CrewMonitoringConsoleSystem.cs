using System.Linq;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.SuitSensors;
using Content.Server.UserInterface;
using Content.Shared.Medical.CrewMonitoring;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Movement.Components;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Medical.CrewMonitoring
{
    public sealed class CrewMonitoringConsoleSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

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
            UpdateUserInterface(uid);
        }

        private void OnUIOpened(EntityUid uid, CrewMonitoringConsoleComponent component, BoundUIOpenedEvent args)
        {
            UpdateUserInterface(uid, component);
        }

        private void UpdateUserInterface(EntityUid uid, CrewMonitoringConsoleComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            var ui = component.Owner.GetUIOrNull(CrewMonitoringUIKey.Key);
            if (ui == null)
                return;

            // For directional arrows, we need to fetch the monitor's transform data
            var xform = Transform(uid);
            var (worldPos, worldRot) = xform.GetWorldPositionRotation();

            // In general, the directions displayed depend on either the orientation of the grid, or the orientation of
            // the monitor. But in the special case where the monitor IS a player (i.e., admin ghost), we base it off
            // the players eye rotation. We don't know what that is for sure, but we know their last grid angle, which
            // should work well enough?
            if (TryComp(uid, out InputMoverComponent? mover))
                worldRot = mover.LastGridAngle;
            else if (_mapManager.TryGetGrid(xform.GridUid, out var grid))
                worldRot = grid.WorldRotation;

            // update all sensors info
            var allSensors = component.ConnectedSensors.Values.ToList();
            var uiState = new CrewMonitoringState(allSensors, worldPos, worldRot, component.Snap, component.Precision);
            ui.SetState(uiState);
        }
    }
}
