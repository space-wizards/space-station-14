using System.Diagnostics.CodeAnalysis;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell.Components;
using JetBrains.Annotations;

namespace Content.Shared.PowerCell;

public sealed partial class PowerCellSystem
{
    /// <summary>
    /// Gets the power cell battery inside a power cell slot.
    /// </summary>
    [PublicAPI]
    public bool TryGetBatteryFromSlot(
        Entity<PowerCellSlotComponent?> ent,
        [NotNullWhen(true)] out Entity<PredictedBatteryComponent>? battery)
    {
        if (!Resolve(ent, ref ent.Comp, false))
        {
            battery = null;
            return false;
        }

        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.CellSlotId, out ItemSlot? slot))
        {
            battery = null;
            return false;
        }

        if (!TryComp<PredictedBatteryComponent>(slot.Item, out var batteryComp))
        {
            battery = null;
            return false;
        }

        battery = (slot.Item.Value, batteryComp);
        return true;
    }

    /// <summary>
    /// Returns whether the entity has a slotted battery and charge for the requested action.
    /// </summary>
    /// <param name="ent">The power cell.</param>
    /// <param name="charge">The charge that is needed.</param>
    /// <param name="user">Show a popup to this user with the relevant details if specified.</param>
    /// <param name="predicted">Whether to predict the popup or not.</param>
    [PublicAPI]
    public bool HasCharge(Entity<PowerCellSlotComponent?> ent, float charge, EntityUid? user = null, bool predicted = false)
    {
        if (!TryGetBatteryFromSlot(ent, out var battery))
        {
            if (user == null)
                return false;

            if (predicted)
                _popup.PopupClient(Loc.GetString("power-cell-no-battery"), ent.Owner, user.Value);
            else
                _popup.PopupEntity(Loc.GetString("power-cell-no-battery"), ent.Owner, user.Value);

            return false;
        }

        if (_battery.GetCharge(battery.Value.AsNullable()) < charge)
        {
            if (user == null)
                return false;

            if (predicted)
                _popup.PopupClient(Loc.GetString("power-cell-insufficient"), ent.Owner, user.Value);
            else
                _popup.PopupEntity(Loc.GetString("power-cell-insufficient"), ent.Owner, user.Value);

            return false;
        }

        return true;
    }

    /// <summary>
    /// Tries to use charge from a slotted battery.
    /// </summary>
    /// <param name="ent">The power cell.</param>
    /// <param name="charge">The charge that is needed.</param>
    /// <param name="user">Show a popup to this user with the relevant details if specified.</param>
    /// <param name="predicted">Whether to predict the popup or not.</param>
    [PublicAPI]
    public bool TryUseCharge(Entity<PowerCellSlotComponent?> ent, float charge, EntityUid? user = null, bool predicted = false)
    {
        if (!TryGetBatteryFromSlot(ent, out var battery))
        {
            if (user == null)
                return false;

            if (predicted)
                _popup.PopupClient(Loc.GetString("power-cell-no-battery"), ent.Owner, user.Value);
            else
                _popup.PopupEntity(Loc.GetString("power-cell-no-battery"), ent.Owner, user.Value);

            return false;
        }

        if (!_battery.TryUseCharge((battery.Value, battery), charge))
        {
            if (user == null)
                return false;

            if (predicted)
                _popup.PopupClient(Loc.GetString("power-cell-insufficient"), ent.Owner, user.Value);
            else
                _popup.PopupEntity(Loc.GetString("power-cell-insufficient"), ent.Owner, user.Value);

            return false;
        }
        return true;
    }

    /// <summary>
    /// Gets number of remaining uses for the given charge cost.
    /// </summary>
    /// <param name="ent">The power cell.</param>
    /// <param name="cost">The cost per use.</param>
    [PublicAPI]
    public int GetRemainingUses(Entity<PowerCellSlotComponent?> ent, float cost)
    {
        if (!TryGetBatteryFromSlot(ent, out var battery))
            return 0;

        return _battery.GetRemainingUses(battery.Value.AsNullable(), cost);
    }

    /// <summary>
    /// Gets number of maximum uses at full charge for the given charge cost.
    /// </summary>
    /// <param name="ent">The power cell.</param>
    /// <param name="cost">The cost per use.</param>
    [PublicAPI]
    public int GetMaxUses(Entity<PowerCellSlotComponent?> ent, float cost)
    {
        if (!TryGetBatteryFromSlot(ent, out var battery))
            return 0;

        return _battery.GetMaxUses(battery.Value.AsNullable(), cost);
    }
}
