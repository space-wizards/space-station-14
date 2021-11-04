using System;
using System.Collections.Generic;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Server.WireHacking;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Monitor.Systems
{
    // AtmosMonitorSystem. Grabs all the AtmosAlarmables connected
    // to it via local APC net, and starts sending updates of the
    // current atmosphere. Monitors fire (which always triggers as
    // a danger), and atmos (which triggers based on set thresholds).
    public class AtmosMonitorSystem : EntitySystem
    {
        [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
        [Dependency] private readonly AtmosDeviceSystem _atmosDeviceSystem = default!;
        [Dependency] private readonly DeviceNetworkSystem _deviceNetSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        // Commands
        public const string AtmosMonitorAlarmCmd = "atmos_monitor_alarm_update";
        public const string AtmosMonitorAlarmSyncCmd = "atmos_monitor_alarm_sync";
        public const string AtmosMonitorAlarmResetAllCmd = "atmos_monitor_alarm_reset_all";

        // Packet data
        public const string AtmosMonitorAlarmThresholdTypes = "atmos_monitor_alarm_threshold_types";
        public const string AtmosMonitorAlarmSrc = "atmos_monitor_alarm_source";
        public const string AtmosMonitorAlarmNetMax = "atmos_monitor_alarm_net_max";

        // Frequency (all prototypes that use AtmosMonitor should use this)
        public const int AtmosMonitorApcFreq = 1621;

        public override void Initialize()
        {
            SubscribeLocalEvent<AtmosMonitorComponent, ComponentInit>(OnAtmosMonitorInit);
            SubscribeLocalEvent<AtmosMonitorComponent, ComponentStartup>(OnAtmosMonitorStartup);
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
            if (component.PowerRecvComponent == null
                && component.AtmosDeviceComponent != null)
            {
                _atmosDeviceSystem.LeaveAtmosphere(component.AtmosDeviceComponent);
                return;
            }

            Logger.DebugS("AtmosMonitor", $"{component.Owner.Transform.LocalRotation}");
            _checkPos.Add(uid);
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
            // atmos alarms will first attempt to get the air
            // directly underneath it - if not, then it will
            // instead place itself directly in front of the tile
            // it is facing, and then visually shift itself back
            // via sprite offsets (SS13 style but fuck it)
            //
            // this introduces some Edge Cases (such as people
            // putting air alarms in the wrong direction and making
            // them visually in the wrong spot
            //
            // This could probably be mitigated like so:
            // - Make a construction step that ensure that offsets
            //   don't get set if the offset is facing away from the user
            //   (i.e., the user must be facing the object
            //   in order to complete it)
            //
            // This cannot be mitigated when spawning any atmos monitors,
            // and this also requires a new system potentially
            //
            // (this also potentially issues the issue with creating
            // wall lights)
            //
            // if that doesn't work, then nothing is done about it
            var coords = component.Owner.Transform.Coordinates;

            if (_atmosphereSystem.IsTileAirBlocked(coords))
            {
                Logger.DebugS("AtmosMonitor", $"airblocked, attempting to reposition: {coords}");
                var rotPos = component.Owner.Transform.LocalRotation.RotateVec(new Vector2(0, -1));
                Logger.DebugS("AtmosMonitor", $"worldRot: {component.Owner.Transform.LocalRotation - MathHelper.PiOver2}");
                Logger.DebugS("AtmosMonitor", $"rotPos: {rotPos}");
                component.Owner.Transform.Anchored = false;
                coords = coords.Offset(rotPos);
                Logger.DebugS("AtmosMonitor", $"newCoords: {coords}");
                component.Owner.Transform.Coordinates = coords;

                appearance.SetData("offset", -rotPos);

                component.Owner.Transform.Anchored = true;
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
                        // resync the alert state with the rest
                        // of the network
                        Sync(component);
                    }
                    break;
            }

            if (component.DisplayMaxAlarmInNet)
                if (EntityManager.TryGetComponent(component.Owner.Uid, out SharedAppearanceComponent? appearanceComponent))
                    appearanceComponent.SetData("alarmType", component.HighestAlarmInNetwork());

        }

        private void OnPowerChangedEvent(EntityUid uid, AtmosMonitorComponent component, PowerChangedEvent args)
        {
            if (!args.Powered)
            {
                if (component.AtmosDeviceComponent != null
                    && component.AtmosDeviceComponent.JoinedGrid != null)
                {
                    _atmosDeviceSystem.LeaveAtmosphere(component.AtmosDeviceComponent);
                    component.TileGas = null;
                }

                // clear memory when power cycled
                component.LastAlarmState = AtmosMonitorAlarmType.Normal;
                component.NetworkAlarmStates.Clear();
            }
            else if (args.Powered)
            {
                if (component.AtmosDeviceComponent != null
                    && component.AtmosDeviceComponent.JoinedGrid == null)
                {
                    _atmosDeviceSystem.JoinAtmosphere(component.AtmosDeviceComponent);
                    var coords = component.Owner.Transform.Coordinates;
                    var air = _atmosphereSystem.GetTileMixture(coords);
                    component.TileGas = air;
                }
            }

            if (EntityManager.TryGetComponent(component.Owner.Uid, out SharedAppearanceComponent? appearanceComponent))
            {
                appearanceComponent.SetData("powered", args.Powered);
                appearanceComponent.SetData("alarmType", component.LastAlarmState);
            }
        }

        private void OnFireEvent(EntityUid uid, AtmosMonitorComponent component, TileFireEvent args)
        {
            if (component.PowerRecvComponent == null
                || !component.PowerRecvComponent.Powered)
                return;

            // if we're monitoring for atmos fire, then we make it similar to a smoke detector
            // and just outright trigger a danger event
            //
            // somebody else can reset it :sunglasses:
            if (component.MonitorFire
                && component.LastAlarmState != AtmosMonitorAlarmType.Danger)
                Alert(component, AtmosMonitorAlarmType.Danger, new []{ AtmosMonitorThresholdType.Temperature }); // technically???

            // only monitor state elevation so that stuff gets alarmed quicker during a fire,
            // let the atmos update loop handle when temperature starts to reach different
            // thresholds and different states than normal -> warning -> danger
            if (component.TemperatureThreshold != null
                && component.TemperatureThreshold.CheckThreshold(args.Temperature, out var temperatureState)
                && temperatureState > component.LastAlarmState)
                Alert(component, AtmosMonitorAlarmType.Danger, new []{ AtmosMonitorThresholdType.Temperature });
        }

        private void OnAtmosUpdate(EntityUid uid, AtmosMonitorComponent component, AtmosDeviceUpdateEvent args)
        {
            if (component.PowerRecvComponent == null
                || !component.PowerRecvComponent.Powered)
                return;

            // can't hurt
            // (in case something is making AtmosDeviceUpdateEvents
            // outside the typical device loop)
            if (component.AtmosDeviceComponent == null
                || component.AtmosDeviceComponent.JoinedGrid == null)
                return;

            // if we're not monitoring atmos, don't bother
            if (component.TemperatureThreshold == null
                && component.PressureThreshold == null
                && component.GasThresholds == null)
                return;

            // why is this in update? because transform rotation
            // doesn't occur at startup! wow! :death:
            if (component.TileGas == null)
            {

            }

            /*
            var coords = component.Owner.Transform.Coordinates;
            var air = _atmosphereSystem.GetTileMixture(coords);
            */

            UpdateState(component, component.TileGas);
        }

        // Update checks the current air if it exceeds thresholds of
        // any kind.
        //
        // If any threshold exceeds the other, that threshold
        // immediately replaces the current recorded state.
        //
        // If the threshold does not match the current state
        // of the monitor, it is set in the Alert call.
        private void UpdateState(AtmosMonitorComponent monitor, GasMixture? air)
        {
            if (air == null) return;

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
                Alert(monitor, state, alarmTypes, air);
            }
        }

        // Alerts the network that the state of this monitor has changed.
        // Also changes the look of the monitor, if applicable.
        public void Alert(AtmosMonitorComponent monitor, AtmosMonitorAlarmType state, IEnumerable<AtmosMonitorThresholdType>? alarms = null, GasMixture? air = null)
        {
            monitor.LastAlarmState = state;
            if (EntityManager.TryGetComponent(monitor.Owner.Uid, out SharedAppearanceComponent? appearanceComponent))
                appearanceComponent.SetData("alarmType", monitor.LastAlarmState);

            BroadcastAlertPacket(monitor, alarms);

            if (EntityManager.TryGetComponent(monitor.Owner.Uid, out AtmosAlarmableComponent alarmable)
                && !alarmable.IgnoreAlarms)
                RaiseLocalEvent(monitor.Owner.Uid, new AtmosMonitorAlarmEvent(monitor.LastAlarmState, monitor.HighestAlarmInNetwork()));
            // TODO: Central system that grabs *all* alarms from wired network
        }

        // Resets this single entity. This probably isn't that useful,
        public void Reset(EntityUid uid, AtmosMonitorComponent? monitor = null)
        {
            if (!Resolve(uid, ref monitor)) return;

            Alert(monitor, AtmosMonitorAlarmType.Normal);
        }

        // useful for fire alarms, might be useful for other things in the future
        //
        // Sends a reset command to everything on the network, from this specific entity,
        // with the source of the prototype ID it has. The reset command will resync
        // the state of whatever accepts it with the rest of the network.
        //
        // Sends a single alert to reset anything that was alarmed, while
        // also clearing out the cached network alarm states on the current
        // monitor (which will be repopulated by broadcast pings)
        public void ResetAll(EntityUid uid, AtmosMonitorComponent? monitor = null)
        {
            if (!Resolve(uid, ref monitor)) return;

            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AtmosMonitorAlarmResetAllCmd,
                [AtmosMonitorAlarmSrc] = monitor.Owner.Prototype != null ? monitor.Owner.Prototype.ID : string.Empty
            };

            _deviceNetSystem.QueuePacket(monitor.Owner.Uid, string.Empty, AtmosMonitorApcFreq, payload, true);
            // this will be repopulated anyways
            monitor.NetworkAlarmStates.Clear();

            // final alert will auto-sync this monitor's state
            // to everyone else
            Alert(monitor, AtmosMonitorAlarmType.Normal);
        }

        // Syncs the current state of this monitor
        // to the network. Separate command,
        // to avoid alerting alarmables. (TODO: maybe
        // just cache monitors in other monitors?)
        private void Sync(AtmosMonitorComponent monitor)
        {
            if (!monitor.NetEnabled) return;

            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AtmosMonitorAlarmSyncCmd,
                [DeviceNetworkConstants.CmdSetState] = monitor.LastAlarmState,
                [AtmosMonitorAlarmSrc] = monitor.Owner.Prototype != null ? monitor.Owner.Prototype.ID : string.Empty
            };

            _deviceNetSystem.QueuePacket(monitor.Owner.Uid, string.Empty, AtmosMonitorApcFreq, payload, true);
        }

        // Broadcasts an alert packet to all devices on the network,
        // which consists of the current alarm types,
        // the highest alarm currently cached by this monitor,
        // and the current alarm state of the monitor (so other
        // alarms can sync to it).
        //
        // Alarmables use the highest alarm to ensure that a monitor's
        // state doesn't override if the alarm is lower. The state
        // is synced between monitors the moment a monitor sends out an alarm,
        // or if it is explicitly synced (see ResetAll/Sync).
        private void BroadcastAlertPacket(AtmosMonitorComponent monitor, IEnumerable<AtmosMonitorThresholdType>? alarms = null)
        {
            if (!monitor.NetEnabled) return;

            string source = string.Empty;
            if (alarms == null) alarms = new List<AtmosMonitorThresholdType>();
            if (monitor.Owner.Prototype != null) source = monitor.Owner.Prototype.ID;

            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = AtmosMonitorAlarmCmd,
                [DeviceNetworkConstants.CmdSetState] = monitor.LastAlarmState,
                [AtmosMonitorAlarmNetMax] = monitor.HighestAlarmInNetwork(),
                [AtmosMonitorAlarmThresholdTypes] = alarms,
                [AtmosMonitorAlarmSrc] = source
            };

            _deviceNetSystem.QueuePacket(monitor.Owner.Uid, string.Empty, AtmosMonitorApcFreq, payload, true);
        }

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

    public class AtmosMonitorAlarmEvent : EntityEventArgs
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
