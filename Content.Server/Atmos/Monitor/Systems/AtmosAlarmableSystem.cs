using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Payloads;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.Atmos.Monitor;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Power;
using Content.Shared.Tag;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Systems;

namespace Content.Server.Atmos.Monitor.Systems;

public sealed partial class AtmosAlarmableSystem : DevicePayloadSystem<AtmosAlarmableComponent>
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private AudioSystem _audioSystem = default!;
    [Dependency] private DeviceNetworkSystem _deviceNet = default!;
    [Dependency] private AtmosDeviceNetworkSystem _atmosDevNetSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AtmosAlarmableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AtmosAlarmableComponent, PowerChangedEvent>(OnPowerChange);
    }

    protected override void InitializeDevice()
    {
        base.InitializeDevice();
        SubscribePayload<AtmosAlarmPayload>(OnAlarmPayload);
        SubscribePayload<AtmosAlarmableResetAllPayload>(OnResetAllPayload);
        SubscribePayload<AtmosAlarmableSyncAlertsPayload>(OnSyncAlertsPayload);
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

    private void OnAlarmPayload(Entity<AtmosAlarmableComponent> ent, ref AtmosAlarmPayload payload, ref DeviceNetworkPacketData args)
    {
        if (!CheckTags(ent, ref payload))
            return;

        var (uid, component) = ent;
        var isValid = (payload.TrippedThresholds & component.MonitorAlertTypes) != 0;

        if (!component.NetworkAlarmStates.ContainsKey(args.SenderAddress))
        {
            if (!isValid)
                return;

            component.NetworkAlarmStates.Add(args.SenderAddress, payload.Type);
        }
        else
        {
            // This is because if the alert is no longer valid,
            // it may mean that the threshold we need to look at has
            // been removed from the threshold types passed:
            // basically, we need to reset this state to normal here.
            component.NetworkAlarmStates[args.SenderAddress] = isValid ? payload.Type : AtmosAlarmType.Normal;
        }

        if (!TryGetHighestAlert(uid, out var netMax, component))
        {
            netMax = AtmosAlarmType.Normal;
        }

        TryUpdateAlert(uid, netMax.Value, component);
    }

    private void OnResetAllPayload(Entity<AtmosAlarmableComponent> ent, ref AtmosAlarmableResetAllPayload payload, ref DeviceNetworkPacketData args)
    {
        if (!CheckTags(ent, ref payload))
            return;

        Reset(ent.Owner, ent.Comp);
    }

    private void OnSyncAlertsPayload(Entity<AtmosAlarmableComponent> ent, ref AtmosAlarmableSyncAlertsPayload payload, ref DeviceNetworkPacketData args)
    {
        if (!CheckTags(ent, ref payload))
            return;

        foreach (var (key, alarm) in payload.AlarmStates)
        {
            ent.Comp.NetworkAlarmStates.TryAdd(key, alarm);
            ent.Comp.NetworkAlarmStates[key] = alarm;
        }

        if (TryGetHighestAlert(ent, out var maxAlert, ent.Comp))
        {
            TryUpdateAlert(ent, maxAlert.Value, ent.Comp);
        }
    }

    private bool CheckTags<T>(Entity<AtmosAlarmableComponent> ent, ref T payload) where T : HandledNetworkPayload
    {
        if (ent.Comp.IgnoreAlarms)
            return false;

        if (payload is not AtmosAlarmableSourcePayload sourcePayload)
            return false;

        return sourcePayload.Source.Any(source => ent.Comp.SyncWithTags.Contains(source));
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

        var payload = new AtmosAlarmableSyncAlertsPayload
        {
            AlarmStates = alarmable.NetworkAlarmStates,
            Source = tags.Tags,
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

        var payload = new AtmosAlarmPayload
        {
            Type = alarmType,
            Source = tags.Tags,
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
            var payload = new AtmosMonitorResetPayload();
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
