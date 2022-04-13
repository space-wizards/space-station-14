using Content.Server.Destructible;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.SuitSensors;
using Content.Server.Power.Components;
using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Timing;

namespace Content.Server.Medical.CrewMonitoring;

public sealed class CrewMonitoringServerSystem : EntitySystem
{
    [Dependency] private readonly SuitSensorSystem _sensors = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;

    private const float UpdateRate = 3f;
    private float _updateDif;
    private EntityUid? _activeServer;

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
        _updateDif += frameTime;
        if (_updateDif < UpdateRate)
            return;
        _updateDif = 0f;

        if (_activeServer == null)
        {
            var servers = EntityManager.EntityQuery<CrewMonitoringServerComponent>().GetEnumerator();
            CrewMonitoringServerComponent? server = null;

            //Retrieve the first available server
            while (servers.MoveNext())
            {
                if (servers.Current.Available)
                {
                    server = servers.Current;
                }
            }

            servers.Dispose();

            if (server == null)
                return;

            _activeServer = server.Owner;
            _deviceNetworkSystem.ConnectDevice(server.Owner);
        }

        if (_activeServer == null)
            return;

        UpdateTimeout((EntityUid) _activeServer!);
        BroadcastSensorStatus((EntityUid) _activeServer!);
    }

    /// <summary>
    /// Returns the address of the currently active server if there is one
    /// </summary>
    public bool TryGetActiveServerAddress(out string? address)
    {
        if (_activeServer == null)
        {
            address = null;
            return false;
        }

        address = GetServerAddress((EntityUid) _activeServer!);
        return address != null;
    }

    private string? GetServerAddress(EntityUid uid, DeviceNetworkComponent? device = null)
    {
        return Resolve(uid, ref device)? device.Address : null;
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
    /// Clears the servers sensor status list and clears _activeServer if the server being removed is the one that's currently active
    /// </summary>
    private void OnRemove(EntityUid uid, CrewMonitoringServerComponent component, ComponentRemove args)
    {
        component.SensorStatus.Clear();
        _activeServer = null;
    }

    /// <summary>
    /// Disconnects and clears _activeServer if the server losing power is the currently active one
    /// </summary>
    private void OnPowerChanged(EntityUid uid, CrewMonitoringServerComponent component, PowerChangedEvent args)
    {
        component.Available = args.Powered;
        if (uid != _activeServer || args.Powered)
            return;

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

    /// <summary>
    /// Disconnects a server from the device network and clears the currently active server
    /// </summary>
    private void DisconnectServer(EntityUid uid, CrewMonitoringServerComponent? server = null, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(uid, ref server, ref device))
         return;

        server.SensorStatus.Clear();
        _deviceNetworkSystem.DisconnectDevice(uid, device);
        _activeServer = null;
    }
}
