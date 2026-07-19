using Content.Server.DeviceNetwork.Systems;
using Content.Server.Medical.CrewMonitoring;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Medical.SuitSensors;
using Robust.Shared.Timing;

namespace Content.Server.Medical.SuitSensors;

public sealed partial class SuitSensorSystem : SharedSuitSensorSystem
{
    private static readonly EntityTimerId UpdateTimer = new("update");

    [Dependency] private IEntityTimerManager _timers = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private SingletonDeviceNetServerSystem _singletonServerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SuitSensorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SuitSensorComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnStartup(Entity<SuitSensorComponent> ent, ref ComponentStartup args)
    {
        _timers.SetTimerAt(ent, UpdateTimer, ent.Comp.NextUpdate);
    }

    private void OnTimer(Entity<SuitSensorComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != UpdateTimer)
            return;

        var sensor = ent.Comp;
        sensor.NextUpdate = args.ScheduledTime + sensor.UpdateRate;
        _timers.SetTimerAt(ent, UpdateTimer, sensor.NextUpdate);

        if (!TryComp<DeviceNetworkComponent>(ent, out var device) || device.TransmitFrequency is null ||
            !CheckSensorAssignedStation(ent))
            return;

        // get sensor status
        var status = GetSensorState((ent.Owner, ent.Comp, null));
        if (status == null)
            return;

        // Retrieve active server address if the sensor isn't connected to a server.
        if (sensor.ConnectedServer == null)
        {
            if (!_singletonServerSystem.TryGetActiveServerAddress<CrewMonitoringServerComponent>(sensor.StationId!.Value, out var address))
                return;

            sensor.ConnectedServer = address;
        }

        var payload = SuitSensorToPacket(status);

        // Clear the connected server if its address isn't on the network.
        if (!_deviceNetworkSystem.IsAddressPresent(device.DeviceNetId, sensor.ConnectedServer))
        {
            sensor.ConnectedServer = null;
            return;
        }

        _deviceNetworkSystem.QueuePacket(ent, sensor.ConnectedServer, payload, device: device);
    }
}
