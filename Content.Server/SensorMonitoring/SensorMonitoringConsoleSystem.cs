using Content.Server.Atmos.Monitor.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Generation.Teg;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Power.Generation.Teg;
using Content.Shared.SensorMonitoring;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SensorMonitoring;

public sealed partial class SensorMonitoringConsoleSystem : DevicePayloadSystem<SensorMonitoringConsoleComponent>
{
    // TODO: THIS THING IS HEAVILY WIP AND NOT READY FOR GENERAL USE BY PLAYERS.
    // Some of the issues, off the top of my head:
    // Way too huge network load when opened
    // UI doesn't update properly in cases like adding new streams/devices
    // Deleting connected devices causes exceptions
    // UI sucks. need a way to make basic dashboards like Grafana, and save them.

    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private UserInterfaceSystem _userInterface = default!;
    [Dependency] private EntityQuery<DeviceNetworkComponent> _deviceNetworkQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitUI();

        UpdatesBefore.Add(typeof(UserInterfaceSystem));

        SubscribeLocalEvent<SensorMonitoringConsoleComponent, DeviceListUpdateEvent>(DeviceListUpdated);
        SubscribeLocalEvent<SensorMonitoringConsoleComponent, ComponentStartup>(ConsoleStartup);
        SubscribeLocalEvent<SensorMonitoringConsoleComponent, AtmosDeviceUpdateEvent>(AtmosUpdate);
    }

    protected override void InitializeDevice()
    {
        base.InitializeDevice();
        SubscribePayload<TegSensorPayload>(OnTegReceived);
        SubscribePayload<BatterySensorDataPayload>(OnBatteryReceived);
        SubscribePayload<SensorMonitoringAtmosDataPayload>(OnAtmosDataReceived);
    }

    public override void Update(float frameTime)
    {
        var consoles = EntityQueryEnumerator<SensorMonitoringConsoleComponent>();
        while (consoles.MoveNext(out var entityUid, out var comp))
        {
            UpdateConsole(entityUid, comp);
        }
    }

    private void UpdateConsole(EntityUid uid, SensorMonitoringConsoleComponent comp)
    {
        var minTime = _gameTiming.CurTime - comp.RetentionTime;

        SensorUpdate(uid, comp);

        foreach (var data in comp.Sensors.Values)
        {
            // Cull old data.
            foreach (var stream in data.Streams.Values)
            {
                while (stream.Samples.TryPeek(out var sample) && sample.Time < minTime)
                {
                    stream.Samples.Dequeue();
                }
            }
        }

        UpdateConsoleUI(uid, comp);
    }

    private void ConsoleStartup(EntityUid uid, SensorMonitoringConsoleComponent component, ComponentStartup args)
    {
        if (TryComp(uid, out DeviceListComponent? network))
            UpdateDevices(uid, component, network.Devices, Array.Empty<EntityUid>());
    }

    private void DeviceListUpdated(
        EntityUid uid,
        SensorMonitoringConsoleComponent component,
        DeviceListUpdateEvent args)
    {
        UpdateDevices(uid, component, args.Devices, args.OldDevices);
    }

    private void UpdateDevices(
        EntityUid uid,
        SensorMonitoringConsoleComponent component,
        IEnumerable<EntityUid> newDevices,
        IEnumerable<EntityUid> oldDevices)
    {
        var kept = new HashSet<EntityUid>();

        foreach (var newDevice in newDevices)
        {
            var deviceType = DetectDeviceType(newDevice);
            if (deviceType == SensorDeviceType.Unknown)
                continue;

            kept.Add(newDevice);
            var sensor = component.Sensors.GetOrNew(newDevice);
            sensor.DeviceType = deviceType;
            if (sensor.NetId == 0)
                sensor.NetId = MakeNetId(component);
        }

        foreach (var oldDevice in oldDevices)
        {
            if (kept.Contains(oldDevice))
                continue;

            if (component.Sensors.TryGetValue(oldDevice, out var sensorData))
            {
                component.RemovedSensors.Add(sensorData.NetId);
                component.Sensors.Remove(oldDevice);
            }
        }
    }

    private SensorDeviceType DetectDeviceType(EntityUid entity)
    {
        if (HasComp<TegGeneratorComponent>(entity))
            return SensorDeviceType.Teg;

        if (HasComp<AtmosMonitorComponent>(entity))
            return SensorDeviceType.AtmosSensor;

        if (HasComp<GasThermoMachineComponent>(entity))
            return SensorDeviceType.ThermoMachine;

        if (HasComp<GasVolumePumpComponent>(entity))
            return SensorDeviceType.VolumePump;

        if (HasComp<BatterySensorComponent>(entity))
            return SensorDeviceType.Battery;

        return SensorDeviceType.Unknown;
    }

    private void OnAtmosDataReceived(
        Entity<SensorMonitoringConsoleComponent> ent,
        ref SensorMonitoringAtmosDataPayload payload,
        ref DeviceNetworkPacketData args)
    {
        switch (payload.Payload)
        {
            case AtmosMonitorDataPayload sensor:
                OnAtmosSensorReceived(ent, ref sensor, ref args);
                break;
            case GasThermoMachineDataPayload thermo:
                OnThermoMachineReceived(ent, ref thermo, ref args);
                break;
            case GasVolumePumpDataPayload volPump:
                OnVolumePipeReceived(ent, ref volPump, ref args);
                break;
        }
    }

    private void OnTegReceived(Entity<SensorMonitoringConsoleComponent> ent,
        ref TegSensorPayload payload,
        ref DeviceNetworkPacketData args)
    {
        if (!ent.Comp.Sensors.TryGetValue(args.Sender, out var sensorData))
            return;

        // @formatter:off
        WriteSample(ent, sensorData, "teg_last_generated", SensorUnit.EnergyJ, payload.LastGeneration);
        WriteSample(ent, sensorData, "teg_power",          SensorUnit.PowerW,  payload.PowerOutput);
        if (ent.Comp.DebugStreams)
            WriteSample(ent, sensorData, "teg_ramp_pos", SensorUnit.PowerW, payload.RampPosition);

        WriteSample(ent, sensorData, "teg_circ_a_in_pressure",     SensorUnit.PressureKpa,  payload.CirculatorA.InletPressure);
        WriteSample(ent, sensorData, "teg_circ_a_in_temperature",  SensorUnit.TemperatureK, payload.CirculatorA.InletTemperature);
        WriteSample(ent, sensorData, "teg_circ_a_out_pressure",    SensorUnit.PressureKpa,  payload.CirculatorA.OutletPressure);
        WriteSample(ent, sensorData, "teg_circ_a_out_temperature", SensorUnit.TemperatureK, payload.CirculatorA.OutletTemperature);

        WriteSample(ent, sensorData, "teg_circ_b_in_pressure",     SensorUnit.PressureKpa,  payload.CirculatorB.InletPressure);
        WriteSample(ent, sensorData, "teg_circ_b_in_temperature",  SensorUnit.TemperatureK, payload.CirculatorB.InletTemperature);
        WriteSample(ent, sensorData, "teg_circ_b_out_pressure",    SensorUnit.PressureKpa,  payload.CirculatorB.OutletPressure);
        WriteSample(ent, sensorData, "teg_circ_b_out_temperature", SensorUnit.TemperatureK, payload.CirculatorB.OutletTemperature);
        // @formatter:on
    }

    private void OnAtmosSensorReceived(Entity<SensorMonitoringConsoleComponent> ent,
        ref AtmosMonitorDataPayload payload,
        ref DeviceNetworkPacketData args)
    {
        if (!ent.Comp.Sensors.TryGetValue(args.Sender, out var sensorData))
            return;

        // @formatter:off
        WriteSample(ent, sensorData, "atmo_pressure",    SensorUnit.PressureKpa,  payload.Pressure);
        WriteSample(ent, sensorData, "atmo_temperature", SensorUnit.TemperatureK, payload.Temperature);
        // @formatter:on
    }

    private void OnThermoMachineReceived(Entity<SensorMonitoringConsoleComponent> ent,
        ref GasThermoMachineDataPayload payload,
        ref DeviceNetworkPacketData args)
    {
        if (!ent.Comp.Sensors.TryGetValue(args.Sender, out var sensorData))
            return;

        // @formatter:off
        WriteSample(ent, sensorData, "abs_energy_delta", SensorUnit.EnergyJ, MathF.Abs(payload.EnergyDelta));
        // @formatter:on
    }

    private void OnVolumePipeReceived(Entity<SensorMonitoringConsoleComponent> ent,
        ref GasVolumePumpDataPayload payload,
        ref DeviceNetworkPacketData args)
    {
        if (!ent.Comp.Sensors.TryGetValue(args.Sender, out var sensorData))
            return;

        // @formatter:off
        WriteSample(ent, sensorData, "moles_transferred", SensorUnit.Moles, payload.LastMolesTransferred);
        // @formatter:on
    }

    private void OnBatteryReceived(Entity<SensorMonitoringConsoleComponent> ent,
        ref BatterySensorDataPayload payload,
        ref DeviceNetworkPacketData args)
    {
        if (!ent.Comp.Sensors.TryGetValue(args.Sender, out var sensorData))
            return;

        var batteryData = payload.Data;

        // @formatter:off
        WriteSample(ent, sensorData, "charge",        SensorUnit.EnergyJ, batteryData.Charge);
        WriteSample(ent, sensorData, "charge_max",    SensorUnit.EnergyJ, batteryData.MaxCharge);

        WriteSample(ent, sensorData, "receiving",     SensorUnit.PowerW,  batteryData.Receiving);
        WriteSample(ent, sensorData, "receiving_max", SensorUnit.PowerW,  batteryData.MaxReceiving);

        WriteSample(ent, sensorData, "supplying",     SensorUnit.PowerW,  batteryData.Supplying);
        WriteSample(ent, sensorData, "supplying_max", SensorUnit.PowerW,  batteryData.MaxSupplying);
        // @formatter:on
    }

    private void WriteSample(
        SensorMonitoringConsoleComponent component,
        SensorMonitoringConsoleComponent.SensorData sensorData,
        string streamName,
        SensorUnit unit,
        float value)
    {
        var stream = sensorData.Streams.GetOrNew(streamName);
        stream.Unit = unit;
        if (stream.NetId == 0)
            stream.NetId = MakeNetId(component);

        var time = _gameTiming.CurTime;
        stream.Samples.Enqueue(new SensorSample(time, value));
    }

    private static int MakeNetId(SensorMonitoringConsoleComponent component)
    {
        return ++component.IdCounter;
    }

    private void AtmosUpdate(
        EntityUid uid,
        SensorMonitoringConsoleComponent comp,
        AtmosDeviceUpdateEvent args)
    {
        foreach (var (ent, data) in comp.Sensors)
        {
            // Send network requests for new data!
            HandledNetworkPayload payload;
            switch (data.DeviceType)
            {
                case SensorDeviceType.Teg:
                    payload = new TegSensorSyncPayload();
                    break;
                case SensorDeviceType.AtmosSensor:
                    payload = new AtmosMonitorSyncDataPayload();
                    break;
                case SensorDeviceType.ThermoMachine:
                    payload = new GasThermoMachineSyncDataPayload();
                    break;
                case SensorDeviceType.VolumePump:
                    payload = new GasVolumePumpSyncDataPayload();
                    break;
                default:
                    // Unknown device type, don't do anything.
                    continue;
            }

            var address = _deviceNetworkQuery.GetComponent(ent);
            _deviceNetwork.QueuePacket(uid, address.Address, payload);
        }
    }

    private void SensorUpdate(EntityUid uid, SensorMonitoringConsoleComponent comp)
    {
        foreach (var (ent, data) in comp.Sensors)
        {
            // Send network requests for new data!
            HandledNetworkPayload payload;
            switch (data.DeviceType)
            {
                case SensorDeviceType.Battery:
                    payload = new BatterySensorSyncPayload();
                    break;
                default:
                    // Unknown device type, don't do anything.
                    continue;
            }

            var address = _deviceNetworkQuery.GetComponent(ent);
            _deviceNetwork.QueuePacket(uid, address.Address, payload);
        }
    }
}
