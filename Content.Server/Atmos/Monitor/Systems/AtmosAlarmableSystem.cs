using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Server.Atmos.Monitor.Systems;

public sealed class AtmosAlarmableSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNet = default!;
    [Dependency] private readonly AtmosDeviceNetworkSystem _atmosDevNetSystem = default!;

    /// <summary>
    ///     An alarm. Has three valid states: Normal, Warning, Danger.
    ///     Will attempt to fetch the tags from the alarming entity
    ///     to send over.
    /// </summary>
    public const string AlertCmd = "atmos_alarm";

    public const string AlertSource = "atmos_alarm_source";

    public const string AlertTypes = "atmos_alarm_types";

    /// <summary>
    ///     Syncs alerts from this alarm receiver to other alarm receivers.
    ///     Creates a network effect as a result. Note: if the alert receiver
    ///     is not aware of the device beforehand, it will not sync.
    /// </summary>
    public const string SyncAlerts = "atmos_alarmable_sync_alerts";

    public const string ResetAll = "atmos_alarmable_reset_all";

    public override void Initialize()
    {
        SubscribeLocalEvent<AtmosAlarmableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AtmosAlarmableComponent, DeviceNetworkPacketEvent>(OnPacketRecv);
        SubscribeLocalEvent<AtmosAlarmableComponent, PowerChangedEvent>(OnPowerChange);
    }

    private void OnMapInit(EntityUid uid, AtmosAlarmableComponent component, MapInitEvent args)
    {
        TryUpdateAlert(
            uid,
            TryGetHighestAlert(uid, out var alarm) ? alarm.Value : AtmosAlarmType.Normal,
            component,
            false);
    }

    private void OnPowerChange(EntityUid uid, AtmosAlarmableComponent component, ref PowerChangedEvent args)
    {
        if (!args.Powered)
        {
            Reset(uid, component);
        }
        else
        {
            // sussy
            _atmosDevNetSystem.Register(uid, null);
            _atmosDevNetSystem.Sync(uid, null);

            TryUpdateAlert(
                uid,
                TryGetHighestAlert(uid, out var alarm) ? alarm.Value : AtmosAlarmType.Normal,
                component,
                false);
        }
    }

    private void OnPacketRecv(EntityUid uid, AtmosAlarmableComponent component, DeviceNetworkPacketEvent args)
    {
        if (component.IgnoreAlarms) return;

        if (!EntityManager.TryGetComponent(uid, out DeviceNetworkComponent? netConn))
            return;

        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? cmd)
            || !args.Data.TryGetValue(AlertSource, out HashSet<string>? sourceTags))
        {
            return;
        }

        var isValid = sourceTags.Any(source => component.SyncWithTags.Contains(source));

        if (!isValid)
        {
            return;
        }

        switch (cmd)
        {
            case AlertCmd:
                // Set the alert state, and then cache it so we can calculate
                // the maximum alarm state at all times.
                if (!args.Data.TryGetValue(DeviceNetworkConstants.CmdSetState, out AtmosAlarmType state))
                {
                    break;
                }

                if (args.Data.TryGetValue(AlertTypes, out HashSet<AtmosMonitorThresholdType>? types) && component.MonitorAlertTypes != null)
                {
                    isValid = types.Any(type => component.MonitorAlertTypes.Contains(type));
                }

                if (!component.NetworkAlarmStates.ContainsKey(args.SenderAddress))
                {
                    if (!isValid)
                    {
                        break;
                    }

                    component.NetworkAlarmStates.Add(args.SenderAddress, state);
                }
                else
                {
                    // This is because if the alert is no longer valid,
                    // it may mean that the threshold we need to look at has
                    // been removed from the threshold types passed:
                    // basically, we need to reset this state to normal here.
                    component.NetworkAlarmStates[args.SenderAddress] = isValid ? state : AtmosAlarmType.Normal;
                }

                if (!TryGetHighestAlert(uid, out var netMax, component))
                {
                    netMax = AtmosAlarmType.Normal;
                }

                TryUpdateAlert(uid, netMax.Value, component);

                break;
            case ResetAll:
                Reset(uid, component);
                break;
            case SyncAlerts:
                if (!args.Data.TryGetValue(SyncAlerts,
                        out IReadOnlyDictionary<string, AtmosAlarmType>? alarms))
                {
                    break;
                }

                foreach (var (key, alarm) in alarms)
                {
                    if (!component.NetworkAlarmStates.TryAdd(key, alarm))
                    {
                        component.NetworkAlarmStates[key] = alarm;
                    }
                }

                if (TryGetHighestAlert(uid, out var maxAlert, component))
                {
                    TryUpdateAlert(uid, maxAlert.Value, component);
                }

                break;
        }
    }

    private void TryUpdateAlert(EntityUid uid, AtmosAlarmType type, AtmosAlarmableComponent alarmable, bool sync = true)
    {
        if (alarmable.LastAlarmState == type)
        {
            return;
        }

        if (sync)
        {
            SyncAlertsToNetwork(uid, null, alarmable);
        }

        alarmable.LastAlarmState = type;
        UpdateAppearance(uid, type);
        PlayAlertSound(uid, type, alarmable);
        RaiseLocalEvent(uid, new AtmosAlarmEvent(type), true);
    }

    public void SyncAlertsToNetwork(EntityUid uid, string? address = null, AtmosAlarmableComponent? alarmable = null, TagComponent? tags = null)
    {
        if (!Resolve(uid, ref alarmable, ref tags) || alarmable.ReceiveOnly)
        {
            return;
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = SyncAlerts,
            [SyncAlerts] = alarmable.NetworkAlarmStates,
            [AlertSource] = tags.Tags
        };

        _deviceNet.QueuePacket(uid, address, payload);
    }

    /// <summary>
    ///     Forces this alarmable to have a specific alert. This will not be reset until the alarmable
    ///     is manually reset. This will store the alarmable as a device in its network states.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="alarmType"></param>
    /// <param name="alarmable"></param>
    public void ForceAlert(EntityUid uid, AtmosAlarmType alarmType,
        AtmosAlarmableComponent? alarmable = null, DeviceNetworkComponent? devNet = null, TagComponent? tags = null)
    {
        if (!Resolve(uid, ref alarmable, ref devNet, ref tags))
        {
            return;
        }

        TryUpdateAlert(uid, alarmType, alarmable, false);

        if (alarmable.ReceiveOnly)
        {
            return;
        }

        if (!alarmable.NetworkAlarmStates.TryAdd(devNet.Address, alarmType))
        {
            alarmable.NetworkAlarmStates[devNet.Address] = alarmType;
        }

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = AlertCmd,
            [DeviceNetworkConstants.CmdSetState] = alarmType,
            [AlertSource] = tags.Tags
        };

        _deviceNet.QueuePacket(uid, null, payload);
    }

    /// <summary>
    ///     Resets the state of this alarmable to normal.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="alarmable"></param>
    public void Reset(EntityUid uid, AtmosAlarmableComponent? alarmable = null, TagComponent? tags = null)
    {
        if (!Resolve(uid, ref alarmable, ref tags, false) || alarmable.LastAlarmState == AtmosAlarmType.Normal)
        {
            return;
        }

        alarmable.NetworkAlarmStates.Clear();
        TryUpdateAlert(uid, AtmosAlarmType.Normal, alarmable);

        if (!alarmable.ReceiveOnly)
        {
            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = ResetAll,
                [AlertSource] = tags.Tags
            };

            _deviceNet.QueuePacket(uid, null, payload);
        }
    }

    public void ResetAllOnNetwork(EntityUid uid, AtmosAlarmableComponent? alarmable = null)
    {
        if (!Resolve(uid, ref alarmable) || alarmable.ReceiveOnly)
        {
            return;
        }

        Reset(uid, alarmable);
    }

    /// <summary>
    ///     Tries to get the highest possible alert stored in this alarm.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="alarm"></param>
    /// <param name="alarmable"></param>
    /// <returns></returns>
    public bool TryGetHighestAlert(EntityUid uid, [NotNullWhen(true)] out AtmosAlarmType? alarm,
        AtmosAlarmableComponent? alarmable = null)
    {
        alarm = null;

        if (!Resolve(uid, ref alarmable, false))
        {
            return false;
        }

        foreach (var alarmState in alarmable.NetworkAlarmStates.Values)
        {
            alarm = alarm == null || alarm < alarmState ? alarmState : alarm;
        }

        return alarm != null;
    }

    private void PlayAlertSound(EntityUid uid, AtmosAlarmType alarm, AtmosAlarmableComponent alarmable)
    {
        if (alarm == AtmosAlarmType.Danger)
        {
            _audioSystem.PlayPvs(alarmable.AlarmSound, uid, AudioParams.Default.WithVolume(alarmable.AlarmVolume));
        }
    }

    private void UpdateAppearance(EntityUid uid, AtmosAlarmType alarm)
    {
        _appearance.SetData(uid, AtmosMonitorVisuals.AlarmType, alarm);
    }
}

public sealed class AtmosAlarmEvent : EntityEventArgs
{
    public AtmosAlarmType AlarmType { get; }

    public AtmosAlarmEvent(AtmosAlarmType netMax)
    {
        AlarmType = netMax;
    }
}
