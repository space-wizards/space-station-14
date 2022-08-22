using System.Diagnostics.CodeAnalysis;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.Atmos.Monitor;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.Monitor.Systems
{
    public sealed class AtmosAlarmableSystem : EntitySystem
    {
        [Dependency] private readonly AppearanceSystem _appearance = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly DeviceNetworkSystem _deviceNet = default!;

        /// <summary>
        ///     Syncs alerts from this alarm receiver to other alarm receivers.
        ///     Creates a network effect as a result. Note: if the alert receiver
        ///     is not aware of the device beforehand, it will not sync.
        /// </summary>
        public const string SyncAlerts = "atmos_alarmable_sync_alerts";

        public const string ResetAll = "atmos_alarmable_reset_all";

        public override void Initialize()
        {
            SubscribeLocalEvent<AtmosAlarmableComponent, DeviceNetworkPacketEvent>(OnPacketRecv);
            SubscribeLocalEvent<AtmosAlarmableComponent, PowerChangedEvent>(OnPowerChange);
        }

        private void OnPowerChange(EntityUid uid, AtmosAlarmableComponent component, PowerChangedEvent args)
        {
            if (!args.Powered)
            {
                Reset(uid, component);
            }
        }

        private void OnPacketRecv(EntityUid uid, AtmosAlarmableComponent component, DeviceNetworkPacketEvent args)
        {
            if (component.IgnoreAlarms) return;

            if (!EntityManager.TryGetComponent(uid, out DeviceNetworkComponent? netConn))
                return;

            if (args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? cmd))
            {
                return;
            }

            switch (cmd)
            {
                case AtmosMonitorSystem.AtmosMonitorAlarmCmd:
                    // Set the alert state, and then cache it so we can calculate
                    // the maximum alarm state at all times.
                    if (args.Data.TryGetValue(DeviceNetworkConstants.CmdSetState, out AtmosMonitorAlarmType state))
                    {
                        if (!component.NetworkAlarmStates.ContainsKey(args.SenderAddress))
                        {
                            component.NetworkAlarmStates.Add(args.SenderAddress, state);
                        }
                        else
                        {
                            component.NetworkAlarmStates[args.SenderAddress] = state;
                        }

                        if (!TryGetHighestAlert(uid, out var netMax, component))
                        {
                            netMax = AtmosMonitorAlarmType.Normal;
                        }

                        component.LastAlarmState = netMax.Value;

                        UpdateAppearance(uid, netMax.Value);
                        PlayAlertSound(uid, netMax.Value, component);
                        RaiseLocalEvent(component.Owner, new AtmosMonitorAlarmEvent(state, netMax.Value), true);
                    }
                    break;
                case ResetAll:
                    Reset(uid, component);
                    break;
                case SyncAlerts:
                    // Synchronize alerts, but only if they're already known by this monitor.
                    // This should help eliminate the chain effect, especially with
                    if (!args.Data.TryGetValue(SyncAlerts,
                            out IReadOnlyDictionary<string, AtmosMonitorAlarmType>? alarms))
                    {
                        break;
                    }

                    foreach (var (key, alarm) in alarms)
                    {
                        if (component.NetworkAlarmStates.ContainsKey(key))
                        {
                            component.NetworkAlarmStates[key] = alarm;
                        }
                    }

                    if (TryGetHighestAlert(uid, out var maxAlert, component)
                        && component.LastAlarmState < maxAlert)
                    {
                        component.LastAlarmState = maxAlert.Value;
                        RaiseLocalEvent(uid, new AtmosMonitorAlarmEvent(maxAlert.Value, maxAlert.Value));
                    }

                    break;
            }
        }

        public void SyncAlertsToNetwork(EntityUid uid, string? address = null, AtmosAlarmableComponent? alarmable = null)
        {
            if (!Resolve(uid, ref alarmable) || alarmable.ReceiveOnly)
            {
                return;
            }

            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = SyncAlerts,
                [SyncAlerts] = alarmable.NetworkAlarmStates
            };

            _deviceNet.QueuePacket(uid, address, payload);
        }

        /// <summary>
        ///     Resets the state of this alarmable to normal.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="alarmable"></param>
        public void Reset(EntityUid uid, AtmosAlarmableComponent? alarmable = null)
        {
            if (!Resolve(uid, ref alarmable))
            {
                return;
            }

            alarmable.LastAlarmState = AtmosMonitorAlarmType.Normal;
            alarmable.NetworkAlarmStates.Clear();

            SyncAlertsToNetwork(uid);
            RaiseLocalEvent(uid, new AtmosMonitorAlarmEvent(AtmosMonitorAlarmType.Normal, AtmosMonitorAlarmType.Normal));
        }

        public void ResetAllOnNetwork(EntityUid uid, AtmosAlarmableComponent? alarmable = null)
        {
            if (!Resolve(uid, ref alarmable))
            {
                return;
            }

            alarmable.LastAlarmState = AtmosMonitorAlarmType.Normal;
            alarmable.NetworkAlarmStates.Clear();

            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = ResetAll
            };

            _deviceNet.QueuePacket(uid, null, payload);
        }

        /// <summary>
        ///     Tries to get the highest possible alert stored in this alarm.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="alarm"></param>
        /// <param name="alarmable"></param>
        /// <returns></returns>
        private bool TryGetHighestAlert(EntityUid uid, [NotNullWhen(true)] out AtmosMonitorAlarmType? alarm,
            AtmosAlarmableComponent? alarmable = null)
        {
            alarm = null;

            if (!Resolve(uid, ref alarmable))
            {
                return false;
            }

            foreach (var alarmState in alarmable.NetworkAlarmStates.Values)
            {
                alarm = alarm < alarmState ? alarmState : alarm;
            }

            return alarm != null;
        }

        private void PlayAlertSound(EntityUid uid, AtmosMonitorAlarmType alarm, AtmosAlarmableComponent alarmable)
        {
            if (alarm == AtmosMonitorAlarmType.Danger)
            {
                _audioSystem.PlayPvs(alarmable.AlarmSound, uid, AudioParams.Default.WithVolume(alarmable.AlarmVolume));
            }
        }

        private void UpdateAppearance(EntityUid uid, AtmosMonitorAlarmType alarm)
        {
            _appearance.SetData(uid, AtmosMonitorVisuals.AlarmType, alarm);
        }
    }
}
