using Content.Server.DeviceNetwork.Systems;
using Content.Server.Emp;
using Content.Server.Medical.CrewMonitoring;
using Content.Server.Station.Systems;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.SuitSensors;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Medical.SuitSensors;

public sealed class SuitSensorSystem : SharedSuitSensorSystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuitSensorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SuitSensorComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<SuitSensorComponent, EmpDisabledRemoved>(OnEmpFinished);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var sensors = EntityQueryEnumerator<SuitSensorComponent, DeviceNetworkComponent>();

        while (sensors.MoveNext(out var uid, out var sensor, out var device))
        {
            if (device.TransmitFrequency is null)
                continue;

            // check if sensor is ready to update
            if (curTime < sensor.NextUpdate)
                continue;

            if (!CheckSensorAssignedStation(uid, sensor))
                continue;

            // TODO: This would cause imprecision at different tick rates.
            sensor.NextUpdate = curTime + sensor.UpdateRate;

            // get sensor status
            var status = GetSensorState(uid, sensor);
            if (status == null)
                continue;

            //Retrieve active server address if the sensor isn't connected to a server
            if (sensor.ConnectedServer == null)
            {
                if (!_singletonServerSystem.TryGetActiveServerAddress<CrewMonitoringServerComponent>(sensor.StationId!.Value, out var address))
                    continue;

                sensor.ConnectedServer = address;
            }

            // Send it to the connected server
            var payload = SuitSensorToPacket(status);

            // Clear the connected server if its address isn't on the network
            if (!_deviceNetworkSystem.IsAddressPresent(device.DeviceNetId, sensor.ConnectedServer))
            {
                sensor.ConnectedServer = null;
                continue;
            }

            _deviceNetworkSystem.QueuePacket(uid, sensor.ConnectedServer, payload, device: device);
        }
    }

    /// <summary>
    /// Checks whether the sensor is assigned to a station or not
    /// and tries to assign an unassigned sensor to a station if it's currently on a grid
    /// </summary>
    /// <returns>True if the sensor is assigned to a station or assigning it was successful. False otherwise.</returns>
    public bool CheckSensorAssignedStation(EntityUid uid, SuitSensorComponent sensor)
    {
        if (!sensor.StationId.HasValue && Transform(uid).GridUid == null)
            return false;

        sensor.StationId = _stationSystem.GetOwningStation(uid);
        Dirty(uid, sensor);
        return sensor.StationId.HasValue;
    }

    private void OnMapInit(EntityUid uid, SuitSensorComponent component, MapInitEvent args)
    {
        // Fallback
        component.StationId ??= _stationSystem.GetOwningStation(uid);

        // generate random mode
        if (component.RandomMode)
        {
            //make the sensor mode favor higher levels, except coords.
            var modesDist = new[]
            {
                SuitSensorMode.SensorOff,
                SuitSensorMode.SensorBinary, SuitSensorMode.SensorBinary,
                SuitSensorMode.SensorVitals, SuitSensorMode.SensorVitals, SuitSensorMode.SensorVitals,
                SuitSensorMode.SensorCords, SuitSensorMode.SensorCords
            };
            component.Mode = _random.Pick(modesDist);
        }
        Dirty(uid, component);
    }

    private void OnEmpPulse(EntityUid uid, SuitSensorComponent component, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;

        component.PreviousMode = component.Mode;
        SetSensor((uid, component), SuitSensorMode.SensorOff, null);

        component.PreviousControlsLocked = component.ControlsLocked;
        component.ControlsLocked = true;
        Dirty(uid, component);
    }

    private void OnEmpFinished(EntityUid uid, SuitSensorComponent component, ref EmpDisabledRemoved args)
    {
        SetSensor((uid, component), component.PreviousMode, null);
        component.ControlsLocked = component.PreviousControlsLocked;
    }
}
