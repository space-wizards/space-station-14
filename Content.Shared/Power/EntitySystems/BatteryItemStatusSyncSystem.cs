using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Power.Components;

namespace Content.Shared.Power.EntitySystems;

/// <summary>
/// Keeps <see cref="BatteryItemStatusComponent"/> on items with batteries up to date for item status UI.
/// </summary>
public sealed class BatteryItemStatusSyncSystem : EntitySystem
{
    [Dependency] private readonly PredictedBatterySystem _battery = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PredictedBatteryComponent, PredictedBatteryChargeChangedEvent>(OnBatteryChargeChanged);
        SubscribeLocalEvent<PowerCellSlotComponent, PredictedBatteryChargeChangedEvent>(OnBatteryChargeChanged);
        SubscribeLocalEvent<PowerCellSlotComponent, PowerCellChangedEvent>(OnPowerCellChanged);
    }

    private void OnBatteryChargeChanged(Entity<PredictedBatteryComponent> ent, ref PredictedBatteryChargeChangedEvent args)
    {
        UpdateStatus(ent.Owner);
    }

    private void OnBatteryChargeChanged(Entity<PowerCellSlotComponent> ent, ref PredictedBatteryChargeChangedEvent args)
    {
        UpdateStatus(ent.Owner);
    }

    private void OnPowerCellChanged(Entity<PowerCellSlotComponent> ent, ref PowerCellChangedEvent args)
    {
        UpdateStatus(ent.Owner);
    }

    /// <summary>
    /// Ensures the <see cref="BatteryItemStatusComponent"/> exists for the entity if it has a battery and updates its percent.
    /// Removes the component if no battery is present.
    /// </summary>
    private void UpdateStatus(EntityUid uid)
    {
        var hasBattery = TryGetBatteryCharge(uid, out var current, out var max) ||
            TryGetSlotBatteryCharge(uid, out current, out max);

        // If there is no battery at all, remove the status component.
        if (!hasBattery)
        {
            if (HasComp<BatteryItemStatusComponent>(uid))
                RemComp<BatteryItemStatusComponent>(uid);
            return;
        }

        var comp = EnsureComp<BatteryItemStatusComponent>(uid);
        var percent = max > 0f ? (int)(current / max * 100) : 0;
        comp.ChargePercent = percent;
        Dirty(uid, comp);
    }

    private bool TryGetBatteryCharge(EntityUid uid, out float current, out float max)
    {
        current = 0f;
        max = 0f;

        if (!TryComp<PredictedBatteryComponent>(uid, out var comp))
            return false;

        current = _battery.GetCharge(uid);
        max = comp.MaxCharge;
        return true;
    }

    private bool TryGetSlotBatteryCharge(EntityUid uid, out float current, out float max)
    {
        current = 0f;
        max = 0f;

        if (!TryComp<PowerCellSlotComponent>(uid, out var slot))
            return false;

        if (!_powerCell.TryGetBatteryFromSlot((uid, slot), out var battery))
            return false;

        current = _battery.GetCharge(battery.Value.AsNullable());
        max = battery.Value.Comp.MaxCharge;
        return true;
    }
}
