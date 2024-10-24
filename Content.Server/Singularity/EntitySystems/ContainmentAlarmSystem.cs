using Content.Shared.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.Singularity.EntitySystems;

public sealed class ContainmentAlarmSystem : SharedContainmentAlarmSystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ContainmentAlarmHolderComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ContainmentAlarmHolderComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<ContainmentAlarmHolderComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<ContainmentAlarmHolderComponent, EntRemovedFromContainerMessage>(OnContainerModified);
    }
    private void OnComponentInit(Entity<ContainmentAlarmHolderComponent> ent, ref ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(ent, ContainerName, ent.Comp.AlarmSlot);
    }
    private void OnComponentRemove(Entity<ContainmentAlarmHolderComponent> ent, ref ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(ent, ent.Comp.AlarmSlot);
    }

    private void OnContainerModified(EntityUid ent, ContainmentAlarmHolderComponent alarm, ContainerModifiedMessage args)
    {
        if (!alarm.Initialized) return;

        if (args.Container.ID != alarm.AlarmSlot.ID)
            return;

        if (args.Container.ContainedEntities.Count > 0)
            EnsureComp<ContainmentAlarmComponent>(ent);
        else
            RemCompDeferred<ContainmentAlarmComponent>(ent);
    }
    /// <summary>
    /// Resets the alarm to whatever the current power level is
    /// </summary>
    public void ResetAlarm(EntityUid uid, ContainmentAlarmComponent alarm, int power)
    {
        alarm.LastAlertPowerLevel = power;
    }

    /// <summary>
    /// Given the current power of the generator and what power it was at last time this alarm went off, sends an alert accross the radio with current power levels.
    /// Channel is seperated into AnnouncementChannel and EmergencyAnnouncementChannel, the latter only kicking in at under the EmergencyThreshold.
    /// </summary>
    public void UpdateAlertLevel(Entity<ContainmentAlarmComponent?> ent, int currentPower)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var alarm = ent.Comp;

        if (alarm.LastAlertPowerLevel - currentPower >= alarm.PowerIntervalBetweenAlerts)
        {
            //Alert territory
            var posText = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(ent.Owner));
            var powerText = (double) currentPower / alarm.PowerCap;
            var message = Loc.GetString("comp-containment-alert-field-losing-power", ("location", posText), ("power", powerText));
            _radio.SendRadioMessage(ent, message, alarm.AnnouncementChannel, ent, escapeMarkup: false);
            alarm.LastAlertPowerLevel = currentPower;
        }
    }
    /// <summary>
    /// Sends over the emergency radio the location of where this containment field broke
    /// </summary>
    public void BroadcastContainmentBreak(Entity<ContainmentAlarmComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var alarm = ent.Comp;

        var posText = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(ent.Owner));
        var message = Loc.GetString("comp-containment-alert-field-link-broken", ("location", posText));
        _radio.SendRadioMessage(ent, message, alarm.AnnouncementChannel, ent, escapeMarkup: false);
    }
}
