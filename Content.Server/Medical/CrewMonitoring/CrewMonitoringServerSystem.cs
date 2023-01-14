using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.SuitSensors;
using Content.Server.Power.Components;
using Content.Server.Station.Systems;
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Timing;

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringServerSystem : EntitySystem
{
    [Dependency] private readonly SuitSensorSystem _sensors = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    private const float UpdateRate = 3f;
    private float _updateDiff;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CrewMonitoringServerComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<CrewMonitoringServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<CrewMonitoringServerComponent, PowerChangedEvent>(OnPowerChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // check update rate
        _updateDiff += frameTime;
        if (_updateDiff < UpdateRate)
            return;
        _updateDiff -= UpdateRate;

        var servers = EntityManager.EntityQuery<CrewMonitoringServerComponent>();
        List<EntityUid> activeServers = new();

        foreach (var server in servers)
        {
            //Make sure the server is disconnected when it becomes unavailable
            if (!server.Available)
            {
                if (server.Active)
                    DisconnectServer(server.Owner, server);

                continue;
            }

            if (!server.Active)
                continue;

            activeServers.Add(server.Owner);
        }

        foreach (var activeServer in activeServers)
        {
            UpdateTimeout(activeServer);
            BroadcastSensorStatus(activeServer);
        }
    }

    /// <summary>
    /// Returns the address of the currently active server for the given station id if there is one
    /// </summary>
    public bool TryGetActiveServerAddress(EntityUid stationId, out string? address)
    {
        var servers = EntityManager.EntityQuery<CrewMonitoringServerComponent, DeviceNetworkComponent>();
        (CrewMonitoringServerComponent, DeviceNetworkComponent)? last = default;

        foreach (var (server, device) in servers)
        {
            if (!_stationSystem.GetOwningStation(server.Owner)?.Equals(stationId) ?? false)
                continue;

            if (!server.Available)
            {
                DisconnectServer(server.Owner,server, device);
                continue;
            }

            last = (server, device);

            if (server.Active)
            {
                address = device.Address;
                return true;
            }
        }

        //If there was no active server for the station make the last available inactive one active
        if (last.HasValue)
        {
            ConnectServer(last.Value.Item1.Owner, last.Value.Item1, last.Value.Item2);
            address = last.Value.Item2.Address;
            return true;
        }

        address = null;
        return address != null;
    }

    /// <summary>
    /// Adds or updates a sensor status entry if the received package is a sensor status update
    /// </summary>
    private void OnPacketReceived(EntityUid uid, CrewMonitoringServerComponent component, DeviceNetworkPacketEvent args)
    {
        var sensorStatus = _sensors.PacketToSuitSensor(args.Data);
        if (sensorStatus == null)
            return;

        sensorStatus.Timestamp = _gameTiming.CurTime;
        component.SensorStatus[args.SenderAddress] = sensorStatus;
    }

    /// <summary>
    /// Clears the servers sensor status list
    /// </summary>
    private void OnRemove(EntityUid uid, CrewMonitoringServerComponent component, ComponentRemove args)
    {
        component.SensorStatus.Clear();
    }

    /// <summary>
    /// Disconnects the server losing power
    /// </summary>
    private void OnPowerChanged(EntityUid uid, CrewMonitoringServerComponent component, ref PowerChangedEvent args)
    {
        component.Available = args.Powered;

        if (!args.Powered)
            DisconnectServer(uid, component);
    }

    /// <summary>
    /// Drop the sensor status if it hasn't been updated for to long
    /// </summary>
    private void UpdateTimeout(EntityUid uid, CrewMonitoringServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        foreach (var (address, sensor) in component.SensorStatus)
        {
            var dif = _gameTiming.CurTime - sensor.Timestamp;
            if (dif.Seconds > component.SensorTimeout)
                component.SensorStatus.Remove(address);
        }
    }

    /// <summary>
    /// Broadcasts the status of all connected sensors
    /// </summary>
    private void BroadcastSensorStatus(EntityUid uid, CrewMonitoringServerComponent? serverComponent = null, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(uid, ref serverComponent, ref device))
            return;

        var payload = new NetworkPayload()
        {
            [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdUpdatedState,
            [SuitSensorConstants.NET_STATUS_COLLECTION] = serverComponent.SensorStatus
        };

        _deviceNetworkSystem.QueuePacket(uid, null, payload, device: device);
    }

    private void ConnectServer(EntityUid uid, CrewMonitoringServerComponent? server = null, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(uid, ref server, ref device))
            return;

        server.Active = true;

        if (_deviceNetworkSystem.IsDeviceConnected(uid, device))
            return;

        _deviceNetworkSystem.ConnectDevice(uid, device);
    }

    /// <summary>
    /// Disconnects a server from the device network and clears the currently active server
    /// </summary>
    private void DisconnectServer(EntityUid uid, CrewMonitoringServerComponent? server = null, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(uid, ref server, ref device))
         return;

        server.SensorStatus.Clear();
        server.Active = false;

        _deviceNetworkSystem.DisconnectDevice(uid, device, false);
    }
}
