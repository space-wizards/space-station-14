using System.Linq;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Monitor.Systems;

// AtmosMonitorSystem. Grabs all the AtmosAlarmables connected
// to it via local APC net, and starts sending updates of the
// current atmosphere. Monitors fire (which always triggers as
// a danger), and atmos (which triggers based on set thresholds).
public sealed class AtmosMonitorSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly AtmosDeviceSystem _atmosDeviceSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    // Commands
    public const string AtmosMonitorSetThresholdCmd = "atmos_monitor_set_threshold";

    // Packet data
    public const string AtmosMonitorThresholdData = "atmos_monitor_threshold_data";

    public const string AtmosMonitorThresholdDataType = "atmos_monitor_threshold_type";

    public const string AtmosMonitorThresholdGasType = "atmos_monitor_threshold_gas";

    public override void Initialize()
    {
        SubscribeLocalEvent<AtmosMonitorComponent, ComponentInit>(OnAtmosMonitorInit);
        SubscribeLocalEvent<AtmosMonitorComponent, ComponentStartup>(OnAtmosMonitorStartup);
        SubscribeLocalEvent<AtmosMonitorComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);
        SubscribeLocalEvent<AtmosMonitorComponent, TileFireEvent>(OnFireEvent);
        SubscribeLocalEvent<AtmosMonitorComponent, PowerChangedEvent>(OnPowerChangedEvent);
        SubscribeLocalEvent<AtmosMonitorComponent, BeforePacketSentEvent>(BeforePacketRecv);
        SubscribeLocalEvent<AtmosMonitorComponent, DeviceNetworkPacketEvent>(OnPacketRecv);
    }

    private void OnAtmosMonitorInit(EntityUid uid, AtmosMonitorComponent component, ComponentInit args)
    {
        if (component.TemperatureThresholdId != null)
            component.TemperatureThreshold = new(_prototypeManager.Index<AtmosAlarmThreshold>(component.TemperatureThresholdId));

        if (component.PressureThresholdId != null)
            component.PressureThreshold = new(_prototypeManager.Index<AtmosAlarmThreshold>(component.PressureThresholdId));

        if (component.GasThresholdIds != null)
        {
            component.GasThresholds = new();
            foreach (var (gas, id) in component.GasThresholdIds)
            {
                if (_prototypeManager.TryIndex<AtmosAlarmThreshold>(id, out var gasThreshold))
                    component.GasThresholds.Add(gas, new(gasThreshold));
            }
        }
    }

    private void OnAtmosMonitorStartup(EntityUid uid, AtmosMonitorComponent component, ComponentStartup args)
    {
        if (!HasComp<ApcPowerReceiverComponent>(uid)
            && TryComp<AtmosDeviceComponent>(uid, out var atmosDeviceComponent))
        {
            _atmosDeviceSystem.LeaveAtmosphere(atmosDeviceComponent);
        }
    }

    private void BeforePacketRecv(EntityUid uid, AtmosMonitorComponent component, BeforePacketSentEvent args)
    {
        if (!component.NetEnabled) args.Cancel();
    }

    private void OnPacketRecv(EntityUid uid, AtmosMonitorComponent component, DeviceNetworkPacketEvent args)
    {
        // sync the internal 'last alarm state' from
        // the other alarms, so that we can calculate
        // the highest network alarm state at any time
        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? cmd))
        {
            return;
        }

        switch (cmd)
        {
            case AtmosDeviceNetworkSystem.RegisterDevice:
                component.RegisteredDevices.Add(args.SenderAddress);
                break;
            case AtmosDeviceNetworkSystem.DeregisterDevice:
                component.RegisteredDevices.Remove(args.SenderAddress);
                break;
            case AtmosAlarmableSystem.ResetAll:
                Reset(uid);
                // Don't clear alarm states here.
                break;
            case AtmosMonitorSetThresholdCmd:
                if (args.Data.TryGetValue(AtmosMonitorThresholdData, out AtmosAlarmThreshold? thresholdData)
                    && args.Data.TryGetValue(AtmosMonitorThresholdDataType, out AtmosMonitorThresholdType? thresholdType))
                {
                    args.Data.TryGetValue(AtmosMonitorThresholdGasType, out Gas? gas);
                    SetThreshold(uid, thresholdType.Value, thresholdData, gas);
                }

                break;
            case AtmosDeviceNetworkSystem.SyncData:
                var payload = new NetworkPayload();
                payload.Add(DeviceNetworkConstants.Command, AtmosDeviceNetworkSystem.SyncData);
                if (component.TileGas != null)
                {
                    var gases = new Dictionary<Gas, float>();
                    foreach (var gas in Enum.GetValues<Gas>())
                    {
                        gases.Add(gas, component.TileGas.GetMoles(gas));
                    }

                    payload.Add(AtmosDeviceNetworkSystem.SyncData, new AtmosSensorData(
                        component.TileGas.Pressure,
                        component.TileGas.Temperature,
                        component.TileGas.TotalMoles,
                        component.LastAlarmState,
                        gases,
                        component.PressureThreshold ?? new(),
                        component.TemperatureThreshold ?? new(),
                        component.GasThresholds ?? new()
                    ));
                }

                _deviceNetSystem.QueuePacket(uid, args.SenderAddress, payload);
                break;
        }
    }

    private void OnPowerChangedEvent(EntityUid uid, AtmosMonitorComponent component, ref PowerChangedEvent args)
    {
        if (TryComp<AtmosDeviceComponent>(uid, out var atmosDeviceComponent))
        {
            if (!args.Powered)
            {
                if (atmosDeviceComponent.JoinedGrid != null)
                {
                    _atmosDeviceSystem.LeaveAtmosphere(atmosDeviceComponent);
                    component.TileGas = null;
                }
            }
            else if (args.Powered)
            {
                if (atmosDeviceComponent.JoinedGrid == null)
                {
                    _atmosDeviceSystem.JoinAtmosphere(atmosDeviceComponent);
                    var air = _atmosphereSystem.GetContainingMixture(uid, true);
                    component.TileGas = air;
                }

                Alert(uid, component.LastAlarmState);
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
            component.TrippedThresholds.Add(AtmosMonitorThresholdType.Temperature);
            Alert(uid, AtmosAlarmType.Danger, null, component); // technically???
        }

        // only monitor state elevation so that stuff gets alarmed quicker during a fire,
        // let the atmos update loop handle when temperature starts to reach different
        // thresholds and different states than normal -> warning -> danger
        if (component.TemperatureThreshold != null
            && component.TemperatureThreshold.CheckThreshold(args.Temperature, out var temperatureState)
            && temperatureState > component.LastAlarmState)
        {
            component.TrippedThresholds.Add(AtmosMonitorThresholdType.Temperature);
            Alert(uid, AtmosAlarmType.Danger, null, component);
        }
    }

    private void OnAtmosUpdate(EntityUid uid, AtmosMonitorComponent component, AtmosDeviceUpdateEvent args)
    {
        if (!this.IsPowered(uid, EntityManager))
            return;

        // can't hurt
        // (in case something is making AtmosDeviceUpdateEvents
        // outside the typical device loop)
        if (!TryComp<AtmosDeviceComponent>(uid, out var atmosDeviceComponent)
            || atmosDeviceComponent.JoinedGrid == null)
            return;

        // if we're not monitoring atmos, don't bother
        if (component.TemperatureThreshold == null
            && component.PressureThreshold == null
            && component.GasThresholds == null)
            return;

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
        HashSet<AtmosMonitorThresholdType> alarmTypes = new(monitor.TrippedThresholds);

        if (monitor.TemperatureThreshold != null
            && monitor.TemperatureThreshold.CheckThreshold(air.Temperature, out var temperatureState))
        {
            if (temperatureState > state)
            {
                state = temperatureState;
                alarmTypes.Add(AtmosMonitorThresholdType.Temperature);
            }
            else if (temperatureState == AtmosAlarmType.Normal)
            {
                alarmTypes.Remove(AtmosMonitorThresholdType.Temperature);
            }
        }

        if (monitor.PressureThreshold != null
            && monitor.PressureThreshold.CheckThreshold(air.Pressure, out var pressureState)
           )
        {
            if (pressureState > state)
            {
                state = pressureState;
                alarmTypes.Add(AtmosMonitorThresholdType.Pressure);
            }
            else if (pressureState == AtmosAlarmType.Normal)
            {
                alarmTypes.Remove(AtmosMonitorThresholdType.Pressure);
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
                alarmTypes.Add(AtmosMonitorThresholdType.Gas);
            }
            else
            {
                alarmTypes.Remove(AtmosMonitorThresholdType.Gas);
            }
        }

        // if the state of the current air doesn't match the last alarm state,
        // we update the state
        if (state != monitor.LastAlarmState || !alarmTypes.SetEquals(monitor.TrippedThresholds))
        {
            Alert(uid, state, alarmTypes, monitor);
        }
    }

    /// <summary>
    ///     Alerts the network that the state of a monitor has changed.
    /// </summary>
    /// <param name="state">The alarm state to set this monitor to.</param>
    /// <param name="alarms">The alarms that caused this alarm state.</param>
    public void Alert(EntityUid uid, AtmosAlarmType state, HashSet<AtmosMonitorThresholdType>? alarms = null, AtmosMonitorComponent? monitor = null)
    {
        if (!Resolve(uid, ref monitor)) return;

        monitor.LastAlarmState = state;
        monitor.TrippedThresholds = alarms ?? monitor.TrippedThresholds;

        BroadcastAlertPacket(monitor);

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
    private void BroadcastAlertPacket(AtmosMonitorComponent monitor, TagComponent? tags = null)
    {
        if (!monitor.NetEnabled) return;

        if (!Resolve(monitor.Owner, ref tags, false))
        {
            return;
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = AtmosAlarmableSystem.AlertCmd,
            [DeviceNetworkConstants.CmdSetState] = monitor.LastAlarmState,
            [AtmosAlarmableSystem.AlertSource] = tags.Tags,
            [AtmosAlarmableSystem.AlertTypes] = monitor.TrippedThresholds
        };

        foreach (var addr in monitor.RegisteredDevices)
        {
            _deviceNetSystem.QueuePacket(monitor.Owner, addr, payload);
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
        if (!Resolve(uid, ref monitor)) return;

        switch (type)
        {
            case AtmosMonitorThresholdType.Pressure:
                monitor.PressureThreshold = threshold;
                break;
            case AtmosMonitorThresholdType.Temperature:
                monitor.TemperatureThreshold = threshold;
                break;
            case AtmosMonitorThresholdType.Gas:
                if (gas == null || monitor.GasThresholds == null) return;
                monitor.GasThresholds[(Gas) gas] = threshold;
                break;
        }

    }
}
