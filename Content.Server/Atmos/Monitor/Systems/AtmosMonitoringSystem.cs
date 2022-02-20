using System.Collections.Generic;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Monitor.Systems
{
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
        /// <summary>
        ///     Command to alarm the network that something has happened.
        /// </summary>
        public const string AtmosMonitorAlarmCmd = "atmos_monitor_alarm_update";

        /// <summary>
        ///     Command to sync this monitor's alarm state with the rest of the network.
        /// </summary>
        public const string AtmosMonitorAlarmSyncCmd = "atmos_monitor_alarm_sync";

        /// <summary>
        ///     Command to reset all alarms on a network.
        /// </summary>
        public const string AtmosMonitorAlarmResetAllCmd = "atmos_monitor_alarm_reset_all";

        // Packet data
        /// <summary>
        ///     Data response that contains the threshold types in an atmos monitor alarm.
        /// </summary>
        public const string AtmosMonitorAlarmThresholdTypes = "atmos_monitor_alarm_threshold_types";

        /// <summary>
        ///     Data response that contains the source of an atmos alarm.
        /// </summary>
        public const string AtmosMonitorAlarmSrc = "atmos_monitor_alarm_source";

        /// <summary>
        ///     Data response that contains the maximum alarm in an atmos alarm network.
        /// </summary>
        public const string AtmosMonitorAlarmNetMax = "atmos_monitor_alarm_net_max";

        /// <summary>
        ///     Frequency (all prototypes that use AtmosMonitor should use this)
        /// </summary>
        public const int AtmosMonitorApcFreq = 1621;

        public override void Initialize()
        {
            SubscribeLocalEvent<AtmosMonitorComponent, ComponentInit>(OnAtmosMonitorInit);
            SubscribeLocalEvent<AtmosMonitorComponent, ComponentStartup>(OnAtmosMonitorStartup);
            SubscribeLocalEvent<AtmosMonitorComponent, ComponentShutdown>(OnAtmosMonitorShutdown);
            SubscribeLocalEvent<AtmosMonitorComponent, AtmosDeviceUpdateEvent>(OnAtmosUpdate);
            SubscribeLocalEvent<AtmosMonitorComponent, TileFireEvent>(OnFireEvent);
            SubscribeLocalEvent<AtmosMonitorComponent, PowerChangedEvent>(OnPowerChangedEvent);
            SubscribeLocalEvent<AtmosMonitorComponent, BeforePacketSentEvent>(BeforePacketRecv);
            SubscribeLocalEvent<AtmosMonitorComponent, PacketSentEvent>(OnPacketRecv);
        }

        private void OnAtmosMonitorInit(EntityUid uid, AtmosMonitorComponent component, ComponentInit args)
        {
            if (component.TemperatureThresholdId != null)
                component.TemperatureThreshold = _prototypeManager.Index<AtmosAlarmThreshold>(component.TemperatureThresholdId);

            if (component.PressureThresholdId != null)
                component.PressureThreshold = _prototypeManager.Index<AtmosAlarmThreshold>(component.PressureThresholdId);

            if (component.GasThresholdIds != null)
            {
                component.GasThresholds = new();
                foreach (var (gas, id) in component.GasThresholdIds)
                    if (_prototypeManager.TryIndex<AtmosAlarmThreshold>(id, out var gasThreshold))
                        component.GasThresholds.Add(gas, gasThreshold);
            }
        }

        private void OnAtmosMonitorStartup(EntityUid uid, AtmosMonitorComponent component, ComponentStartup args)
        {
            if (!HasComp<ApcPowerReceiverComponent>(uid)
                && TryComp<AtmosDeviceComponent>(uid, out var atmosDeviceComponent))
            {
                _atmosDeviceSystem.LeaveAtmosphere(atmosDeviceComponent);
                return;
            }

            _checkPos.Add(uid);
        }

        private void OnAtmosMonitorShutdown(EntityUid uid, AtmosMonitorComponent component, ComponentShutdown args)
        {
            if (_checkPos.Contains(uid)) _checkPos.Remove(uid);
        }

        // hackiest shit ever but there's no PostStartup event
        private HashSet<EntityUid> _checkPos = new();

        public override void Update(float frameTime)
        {
            foreach (var uid in _checkPos)
                OpenAirOrReposition(uid);
        }

        private void OpenAirOrReposition(EntityUid uid, AtmosMonitorComponent? component = null, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref component, ref appearance)) return;

            var transform = Transform(component.Owner);
            // atmos alarms will first attempt to get the air
            // directly underneath it - if not, then it will
            // instead place itself directly in front of the tile
            // it is facing, and then visually shift itself back
            // via sprite offsets (SS13 style but fuck it)
            var coords = transform.Coordinates;

            if (_atmosphereSystem.IsTileAirBlocked(coords))
            {

                var rotPos = transform.LocalRotation.RotateVec(new Vector2(0, -1));
                transform.Anchored = false;
                coords = coords.Offset(rotPos);
                transform.Coordinates = coords;

                appearance.SetData("offset", - new Vector2(0, -1));

                transform.Anchored = true;
            }

            GasMixture? air = _atmosphereSystem.GetTileMixture(coords);
            component.TileGas = air;

            _checkPos.Remove(uid);
        }

        private void BeforePacketRecv(EntityUid uid, AtmosMonitorComponent component, BeforePacketSentEvent args)
        {
            if (!component.NetEnabled) args.Cancel();
        }

        private void OnPacketRecv(EntityUid uid, AtmosMonitorComponent component, PacketSentEvent args)
        {
            // sync the internal 'last alarm state' from
            // the other alarms, so that we can calculate
            // the highest network alarm state at any time
            if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? cmd)
                || !EntityManager.TryGetComponent(uid, out AtmosAlarmableComponent? alarmable)
                || !EntityManager.TryGetComponent(uid, out DeviceNetworkComponent netConn))
                return;

            // ignore packets from self, ignore from different frequency
            if (netConn.Address == args.SenderAddress) return;

            switch (cmd)
            {
                // sync on alarm or explicit sync
                case AtmosMonitorAlarmCmd:
                case AtmosMonitorAlarmSyncCmd:
                    if (args.Data.TryGetValue(AtmosMonitorAlarmSrc, out string? src)
                        && alarmable.AlarmedByPrototypes.Contains(src)
                        && args.Data.TryGetValue(DeviceNetworkConstants.CmdSetState, out AtmosMonitorAlarmType state)
                        && !component.NetworkAlarmStates.TryAdd(args.SenderAddress, state))
                        component.NetworkAlarmStates[args.SenderAddress] = state;
                    break;
                case AtmosMonitorAlarmResetAllCmd:
                    if (args.Data.TryGetValue(AtmosMonitorAlarmSrc, out string? resetSrc)
                        && alarmable.AlarmedByPrototypes.Contains(resetSrc))
                    {
                        component.LastAlarmState = AtmosMonitorAlarmType.Normal;
                        component.NetworkAlarmStates.Clear();
                    }
                    break;
            }

            if (component.DisplayMaxAlarmInNet)
            {
                if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearanceComponent))
                    appearanceComponent.SetData("alarmType", component.HighestAlarmInNetwork);

                if (component.HighestAlarmInNetwork == AtmosMonitorAlarmType.Danger) PlayAlertSound(uid, component);
            }

        }

        private void OnPowerChangedEvent(EntityUid uid, AtmosMonitorComponent component, PowerChangedEvent args)
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

                    // clear memory when power cycled
                    component.LastAlarmState = AtmosMonitorAlarmType.Normal;
                    component.NetworkAlarmStates.Clear();
                }
                else if (args.Powered)
                {
                    if (atmosDeviceComponent.JoinedGrid == null)
                    {
                        _atmosDeviceSystem.JoinAtmosphere(atmosDeviceComponent);
                        var coords = Transform(component.Owner).Coordinates;
                        var air = _atmosphereSystem.GetTileMixture(coords);
                        component.TileGas = air;
                    }
                }
            }

            if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearanceComponent))
            {
                appearanceComponent.SetData("powered", args.Powered);
                appearanceComponent.SetData("alarmType", component.LastAlarmState);
            }
        }

        private void OnFireEvent(EntityUid uid, AtmosMonitorComponent component, ref TileFireEvent args)
        {
            if (!TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiverComponent)
                || !powerReceiverComponent.Powered)
                return;

            // if we're monitoring for atmos fire, then we make it similar to a smoke detector
            // and just outright trigger a danger event
            //
            // somebody else can reset it :sunglasses:
            if (component.MonitorFire
                && component.LastAlarmState != AtmosMonitorAlarmType.Danger)
                Alert(uid, AtmosMonitorAlarmType.Danger, new []{ AtmosMonitorThresholdType.Temperature }, component); // technically???

            // only monitor state elevation so that stuff gets alarmed quicker during a fire,
            // let the atmos update loop handle when temperature starts to reach different
            // thresholds and different states than normal -> warning -> danger
            if (component.TemperatureThreshold != null
                && component.TemperatureThreshold.CheckThreshold(args.Temperature, out var temperatureState)
                && temperatureState > component.LastAlarmState)
                Alert(uid, AtmosMonitorAlarmType.Danger, new []{ AtmosMonitorThresholdType.Temperature }, component);
        }

        private void OnAtmosUpdate(EntityUid uid, AtmosMonitorComponent component, AtmosDeviceUpdateEvent args)
        {
            if (!TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiverComponent)
                || !powerReceiverComponent.Powered)
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

            AtmosMonitorAlarmType state = AtmosMonitorAlarmType.Normal;
            List<AtmosMonitorThresholdType> alarmTypes = new();

            if (monitor.TemperatureThreshold != null
                && monitor.TemperatureThreshold.CheckThreshold(air.Temperature, out var temperatureState)
                && temperatureState > state)
            {
                state = temperatureState;
                alarmTypes.Add(AtmosMonitorThresholdType.Temperature);
            }

            if (monitor.PressureThreshold != null
                && monitor.PressureThreshold.CheckThreshold(air.Pressure, out var pressureState)
                && pressureState > state)
            {
                state = pressureState;
                alarmTypes.Add(AtmosMonitorThresholdType.Pressure);
            }

            if (monitor.GasThresholds != null)
            {
                foreach (var (gas, threshold) in monitor.GasThresholds)
                {
                    var gasRatio = air.GetMoles(gas) / air.TotalMoles;
                    if (threshold.CheckThreshold(gasRatio, out var gasState)
                        && gasState > state)
                    {
                        state = gasState;
                        alarmTypes.Add(AtmosMonitorThresholdType.Gas);
                    }
                }
            }

            // if the state of the current air doesn't match the last alarm state,
            // we update the state
            if (state != monitor.LastAlarmState)
            {
                Alert(uid, state, alarmTypes, monitor);
            }
        }

        /// <summary>
        ///     Alerts the network that the state of a monitor has changed.
        /// </summary>
        /// <param name="state">The alarm state to set this monitor to.</param>
        /// <param name="alarms">The alarms that caused this alarm state.</param>
        public void Alert(EntityUid uid, AtmosMonitorAlarmType state, IEnumerable<AtmosMonitorThresholdType>? alarms = null, AtmosMonitorComponent? monitor = null)
        {
            if (!Resolve(uid, ref monitor)) return;
            monitor.LastAlarmState = state;
            if (EntityManager.TryGetComponent(monitor.Owner, out AppearanceComponent? appearanceComponent))
                appearanceComponent.SetData("alarmType", monitor.LastAlarmState);

            BroadcastAlertPacket(monitor, alarms);

            if (state == AtmosMonitorAlarmType.Danger) PlayAlertSound(uid, monitor);

            if (EntityManager.TryGetComponent(monitor.Owner, out AtmosAlarmableComponent alarmable)
                && !alarmable.IgnoreAlarms)
                RaiseLocalEvent(monitor.Owner, new AtmosMonitorAlarmEvent(monitor.LastAlarmState, monitor.HighestAlarmInNetwork));
            // TODO: Central system that grabs *all* alarms from wired network
        }

        private void PlayAlertSound(EntityUid uid, AtmosMonitorComponent? monitor = null)
        {
            if (!Resolve(uid, ref monitor)) return;

            SoundSystem.Play(Filter.Pvs(uid), monitor.AlarmSound.GetSound(), uid, AudioParams.Default.WithVolume(monitor.AlarmVolume));
        }

        /// <summary>
        ///     Resets a single monitor's alarm.
        /// </summary>
        public void Reset(EntityUid uid) =>
            Alert(uid, AtmosMonitorAlarmType.Normal);

        /// <summary>
        ///     Resets a network's alarms, using this monitor as a source.
        /// </summary>
        /// <remarks>
        ///     The resulting packet will have this monitor set as the source, using its prototype ID if it has one - otherwise just sending an empty string.
        /// </remarks>
        public void ResetAll(EntityUid uid, AtmosMonitorComponent? monitor = null)
        {
            if (!Resolve(uid, ref monitor)) return;

            var prototype = Prototype(monitor.Owner);
            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AtmosMonitorAlarmResetAllCmd,
                [AtmosMonitorAlarmSrc] = prototype != null ? prototype.ID : string.Empty
            };

            _deviceNetSystem.QueuePacket(monitor.Owner, string.Empty, AtmosMonitorApcFreq, payload, true);
            monitor.NetworkAlarmStates.Clear();

            Alert(uid, AtmosMonitorAlarmType.Normal, null, monitor);
        }

        // (TODO: maybe just cache monitors in other monitors?)
        /// <summary>
        ///     Syncs the current state of this monitor to the network (to avoid alerting other monitors).
        /// </summary>
        private void Sync(AtmosMonitorComponent monitor)
        {
            if (!monitor.NetEnabled) return;

            var prototype = Prototype(monitor.Owner);
            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AtmosMonitorAlarmSyncCmd,
                [DeviceNetworkConstants.CmdSetState] = monitor.LastAlarmState,
                [AtmosMonitorAlarmSrc] = prototype != null ? prototype.ID : string.Empty
            };

            _deviceNetSystem.QueuePacket(monitor.Owner, string.Empty, AtmosMonitorApcFreq, payload, true);
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
        private void BroadcastAlertPacket(AtmosMonitorComponent monitor, IEnumerable<AtmosMonitorThresholdType>? alarms = null)
        {
            if (!monitor.NetEnabled) return;

            string source = string.Empty;
            if (alarms == null) alarms = new List<AtmosMonitorThresholdType>();
            var prototype = Prototype(monitor.Owner);
            if (prototype != null) source = prototype.ID;

            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AtmosMonitorAlarmCmd,
                [DeviceNetworkConstants.CmdSetState] = monitor.LastAlarmState,
                [AtmosMonitorAlarmNetMax] = monitor.HighestAlarmInNetwork,
                [AtmosMonitorAlarmThresholdTypes] = alarms,
                [AtmosMonitorAlarmSrc] = source
            };

            _deviceNetSystem.QueuePacket(monitor.Owner, string.Empty, AtmosMonitorApcFreq, payload, true);
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

    public sealed class AtmosMonitorAlarmEvent : EntityEventArgs
    {
        public AtmosMonitorAlarmType Type { get; }
        public AtmosMonitorAlarmType HighestNetworkType { get; }

        public AtmosMonitorAlarmEvent(AtmosMonitorAlarmType type, AtmosMonitorAlarmType netMax)
        {
            Type = type;
            HighestNetworkType = netMax;
        }
    }
}
