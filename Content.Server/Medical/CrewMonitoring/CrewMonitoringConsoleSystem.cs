using System.Linq;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.SuitSensors;
using Content.Server.UserInterface;
using Content.Shared.Medical.CrewMonitoring;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Medical.CrewMonitoring
{
    public sealed class CrewMonitoringConsoleSystem : EntitySystem
    {
        [Dependency] private readonly SuitSensorSystem _sensors = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;

        private const float UpdateRate = 3f;
        private float _updateDif;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CrewMonitoringConsoleComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<CrewMonitoringConsoleComponent, PacketSentEvent>(OnPacketReceived);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // check update rate
            _updateDif += frameTime;
            if (_updateDif < UpdateRate)
                return;
            _updateDif = 0f;

            var consoles = EntityManager.EntityQuery<CrewMonitoringConsoleComponent>();
            foreach (var console in consoles)
            {
                UpdateTimeouts(console.Owner, console);
                UpdateUserInterface(console.Owner, console);
            }
        }

        private void OnRemove(EntityUid uid, CrewMonitoringConsoleComponent component, ComponentRemove args)
        {
            component.ConnectedSensors.Clear();
        }

        private void OnPacketReceived(EntityUid uid, CrewMonitoringConsoleComponent component, PacketSentEvent args)
        {
            var suitSensor = _sensors.PacketToSuitSensor(args.Data);
            if (suitSensor == null)
                return;

            suitSensor.Timestamp = _gameTiming.CurTime;
            component.ConnectedSensors[args.SenderAddress] = suitSensor;
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

            // if the entity is on a grid, use the grid rotation rather than the monitor's rotation.
            if (_mapManager.TryGetGrid(xform.GridID, out var grid))
                worldRot = grid.WorldRotation;

            // update all sensors info
            var allSensors = component.ConnectedSensors.Values.ToList();
            var uiState = new CrewMonitoringState(allSensors, worldPos, worldRot, component.Snap, component.Precision);
            ui.SetState(uiState);
        }

        private void UpdateTimeouts(EntityUid uid, CrewMonitoringConsoleComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            foreach (var (address, sensor) in component.ConnectedSensors)
            {
                // if too many time passed - sensor just dropped connection
                var dif = _gameTiming.CurTime - sensor.Timestamp;
                if (dif.Seconds > component.SensorTimeout)
                    component.ConnectedSensors.Remove(address);
            }
        }
    }
}
