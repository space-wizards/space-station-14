using Content.Server.Weapons.Ranged.Components;
using Content.Server.PowerCell;
using Content.Server.Power.Components;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell.Components;
using Content.Shared.Item;

namespace Content.Server.Power.EntitySystems;

/// <summary>
/// Keeps <see cref="BatteryItemStatusComponent"/> on items with batteries up to date for item status UI.
/// </summary>
public sealed class BatteryItemStatusSyncSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemComponent, MapInitEvent>(OnItemMapInit);
        SubscribeLocalEvent<BatteryComponent, ChargeChangedEvent>(OnBatteryChargeChanged);
        SubscribeLocalEvent<PowerCellSlotComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }

    private void OnItemMapInit(EntityUid uid, ItemComponent _, MapInitEvent __)
    {
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

        UpdateStatus(uid);
    }

    /// <summary>
    /// Ensures the <see cref="BatteryItemStatusComponent"/> exists for the entity if it has a battery and updates its percent.
    /// Removes the component if no battery is present.
    /// </summary>
    private void UpdateStatus(EntityUid uid)
    {
        // Do not add battery status to guns that already show an ammo counter.
        if (HasComp<AmmoCounterComponent>(uid))
            return;

        var hasBattery = TryGetDirectBatteryCharge(uid, out var current, out var max) ||
            TryGetSlottedBatteryCharge(uid, out current, out max);

        // If there is no battery at all, remove the status component.
        if (!hasBattery)
        {
            if (HasComp<BatteryItemStatusComponent>(uid))
                RemComp<BatteryItemStatusComponent>(uid);
            return;
        }

        var comp = EnsureComp<BatteryItemStatusComponent>(uid);
        var percent = max > 0f ? (int)(current / max * 100) : 0;

        if (percent == comp.ChargePercent)
            return;

        comp.ChargePercent = percent;
        Dirty(uid, comp);
    }

    private bool TryGetDirectBatteryCharge(EntityUid uid, out float current, out float max)
    {
        current = 0f;
        max = 0f;

        if (!TryComp<BatteryComponent>(uid, out _))
            return false;

        var get = new GetChargeEvent();
        RaiseLocalEvent(uid, ref get);
        current = get.CurrentCharge;
        max = get.MaxCharge;
        return true;
    }

    private bool TryGetSlottedBatteryCharge(EntityUid uid, out float current, out float max)
    {
        current = 0f;
        max = 0f;

        if (!TryComp<PowerCellSlotComponent>(uid, out var slot))
            return false;

        if (!_powerCell.TryGetBatteryFromSlot(uid, out var batteryEnt, out _, slot) || batteryEnt == null)
            return false;

        var get = new GetChargeEvent();
        RaiseLocalEvent(batteryEnt.Value, ref get);
        current = get.CurrentCharge;
        max = get.MaxCharge;
        return true;
    }
}
