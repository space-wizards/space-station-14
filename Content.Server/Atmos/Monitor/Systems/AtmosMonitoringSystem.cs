using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Payloads;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.Nodes;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.SensorMonitoring;
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Piping.Components;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Power;
using Content.Shared.Tag;

namespace Content.Server.Atmos.Monitor.Systems;

// AtmosMonitorSystem. Grabs all the AtmosAlarmables connected
// to it via local APC net, and starts sending updates of the
// current atmosphere. Monitors fire (which always triggers as
// a danger), and atmos (which triggers based on set thresholds).
public sealed partial class AtmosMonitorSystem : BeforeDevicePayloadSystem<AtmosMonitorComponent>
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private AtmosDeviceSystem _atmosDeviceSystem = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetSystem = default!;
    [Dependency] private NodeContainerSystem _nodeContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AtmosMonitorComponent, ComponentStartup>(OnAtmosMonitorStartup);
        SubscribeLocalEvent<AtmosMonitorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AtmosMonitorComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);
        SubscribeLocalEvent<AtmosMonitorComponent, TileFireEvent>(OnFireEvent);
        SubscribeLocalEvent<AtmosMonitorComponent, PowerChangedEvent>(OnPowerChangedEvent);
        SubscribeLocalEvent<AtmosMonitorComponent, BeforePacketSentEvent>(BeforePacketRecv);
        SubscribeLocalEvent<AtmosMonitorComponent, AtmosDeviceDisabledEvent>(OnAtmosDeviceLeaveAtmosphere);
        SubscribeLocalEvent<AtmosMonitorComponent, AtmosDeviceEnabledEvent>(OnAtmosDeviceEnterAtmosphere);
        SubscribeLocalEvent<AtmosMonitorComponent, AtmosDeviceTileChangedEvent>(OnAtmosDeviceTileChangedEvent);
    }

    protected override void InitializeDevice()
    {
        base.InitializeDevice();
        SubscribePayload<AtmosMonitorRegisterDevicePayload>(OnRegisterDevice);
        SubscribePayload<AtmosMonitorDeregisterDevicePayload>(OnDeregisterDevice);
        SubscribePayload<AtmosMonitorResetPayload>(OnReset);
        SubscribePayload<AtmosMonitorSetThresholdPayload>(OnSetThreshold);
        SubscribePayload<AtmosMonitorSetAllThresholdsPayload>(OnSetAllThresholds);
        SubscribePayload<AtmosMonitorSyncDataPayload>(OnSyncPayload);
    }

    private void OnAtmosDeviceTileChangedEvent(Entity<AtmosMonitorComponent> ent, ref AtmosDeviceTileChangedEvent args)
    {
        if (!ent.Comp.MonitorsPipeNet)
            ent.Comp.TileGas = _atmosphereSystem.GetContainingMixture(ent.Owner, true);
    }

    private void OnAtmosDeviceLeaveAtmosphere(EntityUid uid, AtmosMonitorComponent atmosMonitor, ref AtmosDeviceDisabledEvent args)
    {
        atmosMonitor.TileGas = null;
    }

    private void OnAtmosDeviceEnterAtmosphere(EntityUid uid, AtmosMonitorComponent atmosMonitor, ref AtmosDeviceEnabledEvent args)
    {
        if (atmosMonitor.MonitorsPipeNet && _nodeContainerSystem.TryGetNode<PipeNode>(uid, atmosMonitor.NodeNameMonitoredPipe, out var pipeNode))
        {
            atmosMonitor.TileGas = pipeNode.Air;
            return;
        }

        atmosMonitor.TileGas = _atmosphereSystem.GetContainingMixture(uid, true);
    }

    private void OnMapInit(EntityUid uid, AtmosMonitorComponent component, MapInitEvent args)
    {
        if (component.TemperatureThresholdId != null)
        {
            var proto = ProtoMan.Index<AtmosAlarmThresholdPrototype>(component.TemperatureThresholdId);
            component.TemperatureThreshold ??= new(proto);
        }

        if (component.PressureThresholdId != null)
        {
            var proto = ProtoMan.Index<AtmosAlarmThresholdPrototype>(component.PressureThresholdId);
            component.PressureThreshold ??= new(proto);
        }

        if (component.GasThresholdPrototypes == null)
            return;

        component.GasThresholds ??= new();
        foreach (var (gas, id) in component.GasThresholdPrototypes)
        {
            var proto = ProtoMan.Index<AtmosAlarmThresholdPrototype>(id);
            component.GasThresholds.TryAdd(gas, new(proto));
        }
    }

    private void OnAtmosMonitorStartup(EntityUid uid, AtmosMonitorComponent component, ComponentStartup args)
    {
        if (!HasComp<ApcPowerReceiverComponent>(uid)
            && TryComp<AtmosDeviceComponent>(uid, out var atmosDeviceComponent))
        {
            _atmosDeviceSystem.LeaveAtmosphere((uid, atmosDeviceComponent));
        }
    }

    private void BeforePacketRecv(Entity<AtmosMonitorComponent> ent, ref BeforePacketSentEvent args)
    {
        if (!ent.Comp.NetEnabled)
            args.Cancelled = true;
    }

    private void OnRegisterDevice(Entity<AtmosMonitorComponent> ent, ref AtmosMonitorRegisterDevicePayload payload, ref DeviceNetworkPacketData args)
    {
        ent.Comp.RegisteredDevices.Add(args.SenderAddress);
    }

    private void OnDeregisterDevice(Entity<AtmosMonitorComponent> ent, ref AtmosMonitorDeregisterDevicePayload payload, ref DeviceNetworkPacketData args)
    {
        ent.Comp.RegisteredDevices.Remove(args.SenderAddress);
    }

    private void OnReset(Entity<AtmosMonitorComponent> ent, ref AtmosMonitorResetPayload payload, ref DeviceNetworkPacketData args)
    {
        Reset(ent);
    }

    private void OnSetThreshold(Entity<AtmosMonitorComponent> ent, ref AtmosMonitorSetThresholdPayload payload, ref DeviceNetworkPacketData args)
    {
        SetThreshold(ent, payload.Type, payload.Threshold, payload.Gas);
    }

    private void OnSetAllThresholds(Entity<AtmosMonitorComponent> ent, ref AtmosMonitorSetAllThresholdsPayload payload, ref DeviceNetworkPacketData args)
    {
        SetAllThresholds(ent, payload.Data);
    }

    private void OnSyncPayload(Entity<AtmosMonitorComponent> ent, ref AtmosMonitorSyncDataPayload payload, ref DeviceNetworkPacketData args)
    {
        var dataPayload = new AtmosMonitorDataPayload();
        if (ent.Comp.TileGas != null)
        {
            var gases = new Dictionary<Gas, float>();
            foreach (var gas in Enum.GetValues<Gas>())
            {
                gases.Add(gas, ent.Comp.TileGas.GetMoles(gas));
            }

            dataPayload = new AtmosMonitorDataPayload(
                ent.Comp.TileGas.Pressure,
                ent.Comp.TileGas.Temperature,
                ent.Comp.TileGas.TotalMoles,
                ent.Comp.LastAlarmState,
                gases,
                ent.Comp.PressureThreshold ?? new(),
                ent.Comp.TemperatureThreshold ?? new(),
                ent.Comp.GasThresholds ?? new());
        }

        // TODO consider reworking sensor monitor so it relays the info from Air Alarms
        var airAlarm = new AirAlarmSetDataPayload
        {
            Payload = dataPayload,
        };
        var sensor = new SensorMonitoringAtmosDataPayload
        {
            Payload = dataPayload,
        };

        _deviceNetSystem.QueuePacket(ent.Owner, args.SenderAddress, airAlarm);
        _deviceNetSystem.QueuePacket(ent.Owner, args.SenderAddress, sensor);
        Alert(ent, ent.Comp.LastAlarmState);
    }

    private void OnPowerChangedEvent(Entity<AtmosMonitorComponent> ent, ref PowerChangedEvent args)
    {
        if (TryComp<AtmosDeviceComponent>(ent, out var atmosDeviceComponent))
        {
            if (!args.Powered)
            {
                _atmosDeviceSystem.LeaveAtmosphere((ent, atmosDeviceComponent));
            }
            else
            {
                _atmosDeviceSystem.JoinAtmosphere((ent, atmosDeviceComponent));
                Alert(ent, ent.Comp.LastAlarmState);
            }
        }
    }

    private void OnFireEvent(EntityUid uid, AtmosMonitorComponent component, ref TileFireEvent args)
    {
        if (!this.IsPowered(uid, EntityManager))
            return;

        // if we're monitoring for atmos fire, then we make it similar to a smoke detector
        // and just outright trigger a danger event
        //
        // somebody else can reset it :sunglasses:
        if (component.MonitorFire
            && component.LastAlarmState != AtmosAlarmType.Danger)
        {
            component.TrippedThresholds |= AtmosMonitorThresholdTypeFlags.Temperature;
            Alert(uid, AtmosAlarmType.Danger, null, component); // technically???
        }

        // only monitor state elevation so that stuff gets alarmed quicker during a fire,
        // let the atmos update loop handle when temperature starts to reach different
        // thresholds and different states than normal -> warning -> danger
        if (component.TemperatureThreshold != null
            && component.TemperatureThreshold.CheckThreshold(args.Temperature, out var temperatureState)
            && temperatureState > component.LastAlarmState)
        {
            component.TrippedThresholds |= AtmosMonitorThresholdTypeFlags.Temperature;
            Alert(uid, AtmosAlarmType.Danger, null, component);
        }
    }

    private void OnAtmosUpdate(EntityUid uid, AtmosMonitorComponent component, ref AtmosDeviceUpdateEvent args)
    {
        if (!this.IsPowered(uid, EntityManager))
            return;

        if (args.Grid == null)
            return;

        // if we're not monitoring atmos, don't bother
        if (component.TemperatureThreshold == null
            && component.PressureThreshold == null
            && component.GasThresholds == null)
            return;

        // If monitoring a pipe network, get its most recent gas mixture
        if (component.MonitorsPipeNet && _nodeContainerSystem.TryGetNode<PipeNode>(uid, component.NodeNameMonitoredPipe, out var pipeNode))
            component.TileGas = pipeNode.Air;

        UpdateState(uid, component.TileGas, component);
    }

    // Update checks the current air if it exceeds thresholds of
    // any kind.
    //
    // If any threshold exceeds the other, that threshold
    // immediately replaces the current recorded state.
    //
    // If the threshold does not match the current state
    // of the monitor, it is set in the Alert call.
    private void UpdateState(EntityUid uid, GasMixture? air, AtmosMonitorComponent? monitor = null)
    {
        if (air == null) return;

        if (!Resolve(uid, ref monitor)) return;

        var state = AtmosAlarmType.Normal;
        var alarmTypes = monitor.TrippedThresholds;

        if (monitor.TemperatureThreshold != null
            && monitor.TemperatureThreshold.CheckThreshold(air.Temperature, out var temperatureState))
        {
            if (temperatureState > state)
            {
                state = temperatureState;
                alarmTypes |= AtmosMonitorThresholdTypeFlags.Temperature;
            }
            else if (temperatureState == AtmosAlarmType.Normal)
            {
                alarmTypes &= ~AtmosMonitorThresholdTypeFlags.Temperature;
            }
        }

        if (monitor.PressureThreshold != null
            && monitor.PressureThreshold.CheckThreshold(air.Pressure, out var pressureState)
           )
        {
            if (pressureState > state)
            {
                state = pressureState;
                alarmTypes |= AtmosMonitorThresholdTypeFlags.Pressure;
            }
            else if (pressureState == AtmosAlarmType.Normal)
            {
                alarmTypes &= ~AtmosMonitorThresholdTypeFlags.Pressure;
            }
        }

        if (monitor.GasThresholds != null)
        {
            var tripped = false;
            foreach (var (gas, threshold) in monitor.GasThresholds)
            {
                var gasRatio = air.GetMoles(gas) / air.TotalMoles;
                if (threshold.CheckThreshold(gasRatio, out var gasState)
                    && gasState > state)
                {
                    state = gasState;
                    tripped = true;
                }
            }

            if (tripped)
            {
                alarmTypes |= AtmosMonitorThresholdTypeFlags.Gas;
            }
            else
            {
                alarmTypes &= ~AtmosMonitorThresholdTypeFlags.Gas;
            }
        }

        // if the state of the current air doesn't match the last alarm state,
        // we update the state
        if (state != monitor.LastAlarmState || alarmTypes != monitor.TrippedThresholds)
        {
            Alert(uid, state, alarmTypes, monitor);
        }
    }

    /// <summary>
    ///     Alerts the network that the state of a monitor has changed.
    /// </summary>
    /// <param name="state">The alarm state to set this monitor to.</param>
    /// <param name="alarms">The alarms that caused this alarm state.</param>
    public void Alert(EntityUid uid, AtmosAlarmType state, AtmosMonitorThresholdTypeFlags? alarms = null, AtmosMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
            return;

        monitor.LastAlarmState = state;
        monitor.TrippedThresholds = alarms ?? monitor.TrippedThresholds;

        BroadcastAlertPacket((uid, monitor));

        // TODO: Central system that grabs *all* alarms from wired network
    }

    /// <summary>
    ///     Resets a single monitor's alarm.
    /// </summary>
    private void Reset(EntityUid uid)
    {
        Alert(uid, AtmosAlarmType.Normal);
    }

    /// <summary>
    ///	Broadcasts an alert packet to all devices on the network,
    ///	which consists of the current alarm types,
    ///	the highest alarm currently cached by this monitor,
    ///	and the current alarm state of the monitor (so other
    ///	alarms can sync to it).
    /// </summary>
    /// <remarks>
    ///	Alarmables use the highest alarm to ensure that a monitor's
    ///	state doesn't override if the alarm is lower. The state
    ///	is synced between monitors the moment a monitor sends out an alarm,
    ///	or if it is explicitly synced (see ResetAll/Sync).
    /// </remarks>
    private void BroadcastAlertPacket(Entity<AtmosMonitorComponent> ent, TagComponent? tags = null)
    {
        var (owner, monitor) = ent;
        if (!monitor.NetEnabled)
            return;

        if (!Resolve(owner, ref tags, false))
        {
            return;
        }

        var payload = new AtmosAlarmPayload
        {
            Type = monitor.LastAlarmState,
            Source = tags.Tags,
            TrippedThresholds = monitor.TrippedThresholds,
        };

        foreach (var addr in monitor.RegisteredDevices)
        {
            _deviceNetSystem.QueuePacket(owner, addr, payload);
        }
    }

    /// <summary>
    ///     Set a monitor's threshold.
    /// </summary>
    /// <param name="type">The type of threshold to change.</param>
    /// <param name="threshold">Threshold data.</param>
    /// <param name="gas">Gas, if applicable.</param>
    public void SetThreshold(EntityUid uid, AtmosMonitorThresholdType type, AtmosAlarmThreshold threshold, Gas? gas = null, AtmosMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor))
            return;

        // Used for logging after the switch statement
        string logPrefix = "";
        string logValueSuffix = "";
        AtmosAlarmThreshold? logPreviousThreshold = null;

        switch (type)
        {
            case AtmosMonitorThresholdType.Pressure:
                logPrefix = "pressure";
                logValueSuffix = "kPa";
                logPreviousThreshold = monitor.PressureThreshold;

                monitor.PressureThreshold = threshold;
                break;
            case AtmosMonitorThresholdType.Temperature:
                logPrefix = "temperature";
                logValueSuffix = "K";
                logPreviousThreshold = monitor.TemperatureThreshold;

                monitor.TemperatureThreshold = threshold;
                break;
            case AtmosMonitorThresholdType.Gas:
                if (gas == null || monitor.GasThresholds == null)
                    return;

                logPrefix = ((Gas) gas).ToString();
                logValueSuffix = "kPa";
                monitor.GasThresholds.TryGetValue((Gas) gas, out logPreviousThreshold);

                monitor.GasThresholds[(Gas) gas] = threshold;
                break;
        }

        // Admin log each change separately rather than logging the whole state
        if (logPreviousThreshold != null)
        {
            if (threshold.Ignore != logPreviousThreshold.Ignore)
            {
                string enabled = threshold.Ignore ? "disabled" : "enabled";
                _adminLogger.Add(
                    LogType.AtmosDeviceSetting,
                    LogImpact.Medium,
                    $"{ToPrettyString(uid)} {logPrefix} thresholds {enabled}"
                );
            }

            foreach (var change in threshold.GetChanges(logPreviousThreshold))
            {
                if (change.Current.Enabled != change.Previous?.Enabled)
                {
                    string enabled = change.Current.Enabled ? "enabled" : "disabled";
                    _adminLogger.Add(
                        LogType.AtmosDeviceSetting,
                        LogImpact.Medium,
                        $"{ToPrettyString(uid)} {logPrefix} {change.Type} {enabled}"
                    );
                }

                if (change.Current.Value != change.Previous?.Value)
                {
                    _adminLogger.Add(
                        LogType.AtmosDeviceSetting,
                        LogImpact.Medium,
                        $"{ToPrettyString(uid)} {logPrefix} {change.Type} changed from {change.Previous?.Value} {logValueSuffix} to {change.Current.Value} {logValueSuffix}"
                    );
                }
            }
        }
    }

    /// <summary>
    ///     Sets all of a monitor's thresholds at once according to the incoming
    ///     AtmosSensorData object's thresholds.
    /// </summary>
    /// <param name="uid">The entity's uid</param>
    /// <param name="allThresholdDataPayload">An AtmosSensorData object from which the thresholds will be loaded.</param>
    public void SetAllThresholds(EntityUid uid, AtmosMonitorDataPayload allThresholdDataPayload)
    {
        SetThreshold(uid, AtmosMonitorThresholdType.Temperature, allThresholdDataPayload.TemperatureThreshold);
        SetThreshold(uid, AtmosMonitorThresholdType.Pressure, allThresholdDataPayload.PressureThreshold);
        foreach (var gas in Enum.GetValues<Gas>())
        {
            SetThreshold(uid, AtmosMonitorThresholdType.Gas, allThresholdDataPayload.GasThresholds[gas], gas);
        }
    }

    protected override void OnBeforePayload(Entity<AtmosMonitorComponent> ent, ref BeforePacketSentEvent args)
    {
        BeforePacketRecv(ent, ref args);
    }
}
