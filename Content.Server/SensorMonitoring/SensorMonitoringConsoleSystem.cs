using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Generation.Teg;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Piping.Binary.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.SensorMonitoring;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SensorMonitoring;

public sealed partial class SensorMonitoringConsoleSystem : EntitySystem
{
    // TODO: THIS THING IS HEAVILY WIP AND NOT READY FOR GENERAL USE BY PLAYERS.
    // Some of the issues, off the top of my head:
    // Way too huge network load when opened
    // UI doesn't update properly in cases like adding new streams/devices
    // Deleting connected devices causes exceptions
    // UI sucks. need a way to make basic dashboards like Grafana, and save them.

    private EntityQuery<DeviceNetworkComponent> _deviceNetworkQuery;

    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitUI();

        UpdatesBefore.Add(typeof(UserInterfaceSystem));

        SubscribeLocalEvent<SensorMonitoringConsoleComponent, DeviceListUpdateEvent>(DeviceListUpdated);
        SubscribeLocalEvent<SensorMonitoringConsoleComponent, ComponentStartup>(ConsoleStartup);
        SubscribeLocalEvent<SensorMonitoringConsoleComponent, DeviceNetworkPacketEvent>(DevicePacketReceived);
        SubscribeLocalEvent<SensorMonitoringConsoleComponent, AtmosDeviceUpdateEvent>(AtmosUpdate);

        _deviceNetworkQuery = GetEntityQuery<DeviceNetworkComponent>();
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

    private void DevicePacketReceived(EntityUid uid, SensorMonitoringConsoleComponent component,
        DeviceNetworkPacketEvent args)
    {
        if (!component.Sensors.TryGetValue(args.Sender, out var sensorData))
            return;

        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command))
            return;

        switch (sensorData.DeviceType)
        {
            case SensorDeviceType.Teg:
                if (command != TegSystem.DeviceNetworkCommandSyncData)
                    return;

                if (!args.Data.TryGetValue(TegSystem.DeviceNetworkCommandSyncData, out TegSensorData? tegData))
                    return;

                // @formatter:off
                WriteSample(component, sensorData, "teg_last_generated", SensorUnit.EnergyJ, tegData.LastGeneration);
                WriteSample(component, sensorData, "teg_power",          SensorUnit.PowerW,  tegData.PowerOutput);
                if (component.DebugStreams)
                    WriteSample(component, sensorData, "teg_ramp_pos", SensorUnit.PowerW, tegData.RampPosition);

                WriteSample(component, sensorData, "teg_circ_a_in_pressure",     SensorUnit.PressureKpa,  tegData.CirculatorA.InletPressure);
                WriteSample(component, sensorData, "teg_circ_a_in_temperature",  SensorUnit.TemperatureK, tegData.CirculatorA.InletTemperature);
                WriteSample(component, sensorData, "teg_circ_a_out_pressure",    SensorUnit.PressureKpa,  tegData.CirculatorA.OutletPressure);
                WriteSample(component, sensorData, "teg_circ_a_out_temperature", SensorUnit.TemperatureK, tegData.CirculatorA.OutletTemperature);

                WriteSample(component, sensorData, "teg_circ_b_in_pressure",     SensorUnit.PressureKpa,  tegData.CirculatorB.InletPressure);
                WriteSample(component, sensorData, "teg_circ_b_in_temperature",  SensorUnit.TemperatureK, tegData.CirculatorB.InletTemperature);
                WriteSample(component, sensorData, "teg_circ_b_out_pressure",    SensorUnit.PressureKpa,  tegData.CirculatorB.OutletPressure);
                WriteSample(component, sensorData, "teg_circ_b_out_temperature", SensorUnit.TemperatureK, tegData.CirculatorB.OutletTemperature);
                // @formatter:on
                break;

            case SensorDeviceType.AtmosSensor:
                if (command != AtmosDeviceNetworkSystem.SyncData)
                    return;

                if (!args.Data.TryGetValue(AtmosDeviceNetworkSystem.SyncData, out AtmosSensorData? atmosData))
                    return;

                // @formatter:off
                WriteSample(component, sensorData, "atmo_pressure",    SensorUnit.PressureKpa,    atmosData.Pressure);
                WriteSample(component, sensorData, "atmo_temperature", SensorUnit.TemperatureK, atmosData.Temperature);
                // @formatter:on
                break;

            case SensorDeviceType.ThermoMachine:
                if (command != AtmosDeviceNetworkSystem.SyncData)
                    return;

                if (!args.Data.TryGetValue(AtmosDeviceNetworkSystem.SyncData, out GasThermoMachineData? thermoData))
                    return;

                // @formatter:off
                WriteSample(component, sensorData, "abs_energy_delta", SensorUnit.EnergyJ, MathF.Abs(thermoData.EnergyDelta));
                // @formatter:on
                break;

            case SensorDeviceType.VolumePump:
                if (command != AtmosDeviceNetworkSystem.SyncData)
                    return;

                if (!args.Data.TryGetValue(AtmosDeviceNetworkSystem.SyncData, out GasVolumePumpData? volumePumpData))
                    return;

                // @formatter:off
                WriteSample(component, sensorData, "moles_transferred", SensorUnit.Moles, volumePumpData.LastMolesTransferred);
                // @formatter:on
                break;

            case SensorDeviceType.Battery:
                if (command != BatterySensorSystem.DeviceNetworkCommandSyncData)
                    return;

                if (!args.Data.TryGetValue(BatterySensorSystem.DeviceNetworkCommandSyncData, out BatterySensorData? batteryData))
                    return;

                // @formatter:off
                WriteSample(component, sensorData, "charge",        SensorUnit.EnergyJ, batteryData.Charge);
                WriteSample(component, sensorData, "charge_max",    SensorUnit.EnergyJ, batteryData.MaxCharge);

                WriteSample(component, sensorData, "receiving",     SensorUnit.PowerW,  batteryData.Receiving);
                WriteSample(component, sensorData, "receiving_max", SensorUnit.PowerW,  batteryData.MaxReceiving);

                WriteSample(component, sensorData, "supplying",     SensorUnit.PowerW,  batteryData.Supplying);
                WriteSample(component, sensorData, "supplying_max", SensorUnit.PowerW,  batteryData.MaxSupplying);
                // @formatter:on

                break;
        }
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
            NetworkPayload payload;
            switch (data.DeviceType)
            {
                case SensorDeviceType.Teg:
                    payload = new NetworkPayload
                    {
                        [DeviceNetworkConstants.Command] = TegSystem.DeviceNetworkCommandSyncData
                    };
                    break;

                case SensorDeviceType.AtmosSensor:
                case SensorDeviceType.ThermoMachine:
                case SensorDeviceType.VolumePump:
                    payload = new NetworkPayload
                    {
                        [DeviceNetworkConstants.Command] = AtmosDeviceNetworkSystem.SyncData
                    };
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
            NetworkPayload payload;
            switch (data.DeviceType)
            {
                case SensorDeviceType.Battery:
                    payload = new NetworkPayload
                    {
                        [DeviceNetworkConstants.Command] = BatterySensorSystem.DeviceNetworkCommandSyncData
                    };
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
