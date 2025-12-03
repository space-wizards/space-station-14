using Content.Shared.Emp;
using Content.Shared.Kitchen;
using Content.Shared.Power;
using Content.Shared.PowerCell.Components;
using Content.Shared.Rejuvenate;

namespace Content.Shared.PowerCell;

public sealed partial class PowerCellSystem
{
    public void InitializeRelay()
    {
        SubscribeLocalEvent<PowerCellSlotComponent, BeingMicrowavedEvent>(RelayToCell);
        SubscribeLocalEvent<PowerCellSlotComponent, RejuvenateEvent>(RelayToCell);
        SubscribeLocalEvent<PowerCellSlotComponent, GetChargeEvent>(RelayToCell);
        SubscribeLocalEvent<PowerCellSlotComponent, ChangeChargeEvent>(RelayToCell);

        SubscribeLocalEvent<PowerCellComponent, EmpAttemptEvent>(RelayToCellSlot); // Prevent the ninja from EMPing its own battery
        SubscribeLocalEvent<PowerCellComponent, PredictedBatteryChargeChangedEvent>(RelayToCellSlot);
        SubscribeLocalEvent<PowerCellComponent, PredictedBatteryStateChangedEvent>(RelayToCellSlot); // For shutting down devices if the battery is empty
        SubscribeLocalEvent<PowerCellComponent, RefreshChargeRateEvent>(RelayToCellSlot); // Allow devices to charge/drain inserted batteries
    }

    private void RelayToCell<T>(Entity<PowerCellSlotComponent> ent, ref T args) where T : notnull
    {
        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.CellSlotId, out var slot) || !slot.Item.HasValue)
            return;

        // Relay the event to the power cell.
        RaiseLocalEvent(slot.Item.Value, ref args);
    }

    private void RelayToCellSlot<T>(Entity<PowerCellComponent> ent, ref T args) where T : notnull
    {
        var parent = Transform(ent).ParentUid;
        // Relay the event to the slot entity.
        if (HasComp<PowerCellSlotComponent>(parent))
            RaiseLocalEvent(parent, ref args);
    }
}
