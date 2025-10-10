using Content.Shared.Power.Components;
using Content.Server.Power.Components;
using Content.Shared.PowerCell.Components;
using Content.Shared.Item;
using Content.Shared.Power;
using Content.Server.Weapons.Ranged.Components;

namespace Content.Server.Power.EntitySystems;

/// <summary>
/// Keeps <see cref="BatteryItemStatusComponent"/> on items with batteries up to date for item status UI,
/// without polling every frame. Updates are driven by battery charge change events and cell insert/eject.
/// </summary>
public sealed class BatteryItemStatusSyncSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemComponent, MapInitEvent>(OnItemMapInit);
        SubscribeLocalEvent<BatteryComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
        SubscribeLocalEvent<PowerCellSlotComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }

    private void OnItemMapInit(EntityUid uid, ItemComponent _, MapInitEvent __)
    {
        // Do not add battery status to guns that already show an ammo counter.
        if (HasComp<AmmoCounterComponent>(uid))
            return;

        UpdateStatus(uid);
    }

    private void OnBatteryChargeChanged(EntityUid uid, BatteryComponent component, ref ChargeChangedEvent args)
    {
        // Only surface battery status for items.
        if (!HasComp<ItemComponent>(uid))
            return;

        UpdateStatus(uid);
    }

    private void OnPowerCellChanged(EntityUid uid, PowerCellSlotComponent component, PowerCellChangedEvent args)
    {
        // Only surface battery status for items.
        if (!HasComp<ItemComponent>(uid))
            return;

        // Do not add battery status to guns that already show an ammo counter.
        if (HasComp<AmmoCounterComponent>(uid))
            return;

        UpdateStatus(uid);
    }

    /// <summary>
    /// Ensures the <see cref="BatteryItemStatusComponent"/> exists for the entity if it has a battery and updates its percent.
    /// Removes the component if no battery is present.
    /// </summary>
    private void UpdateStatus(EntityUid uid)
    {
        var infoEvent = new GetBatteryInfoEvent();
        RaiseLocalEvent(uid, ref infoEvent);

        if (!infoEvent.HasBattery)
        {
            if (HasComp<BatteryItemStatusComponent>(uid))
                RemComp<BatteryItemStatusComponent>(uid);
            return;
        }

        var comp = EnsureComp<BatteryItemStatusComponent>(uid);
        var percent = (int)(infoEvent.ChargePercent * 100);

        if (percent == comp.ChargePercent)
            return;

        comp.ChargePercent = percent;
        Dirty(uid, comp);
    }
}
