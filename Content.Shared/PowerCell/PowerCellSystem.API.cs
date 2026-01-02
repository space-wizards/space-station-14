using System.Diagnostics.CodeAnalysis;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell.Components;
using JetBrains.Annotations;

namespace Content.Shared.PowerCell;

public sealed partial class PowerCellSystem
{
    /// <summary>
    /// Checks if a power cell slot has a battery inside.
    /// </summary>
    [PublicAPI]
    public bool HasBattery(Entity<PowerCellSlotComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.CellSlotId, out var slot))
        {
            return false;
        }

        return slot.Item != null;
    }

    /// <summary>
    /// Gets the power cell battery inside a power cell slot.
    /// </summary>
    [PublicAPI]
    public bool TryGetBatteryFromSlot(
        Entity<PowerCellSlotComponent?> ent,
        [NotNullWhen(true)] out Entity<BatteryComponent>? battery)
    {
        if (!Resolve(ent, ref ent.Comp, false))
        {
            battery = null;
            return false;
        }

        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.CellSlotId, out var slot))
        {
            battery = null;
            return false;
        }

        if (!TryComp<BatteryComponent>(slot.Item, out var batteryComp))
        {
            battery = null;
            return false;
        }

        battery = (slot.Item.Value, batteryComp);
        return true;
    }

    /// <summary>
    /// First tries to get a battery from the entity's power cell slot.
    /// If that fails check if the entity itself is a battery with <see cref="BatteryComponent"/>.
    /// </summary>
    [PublicAPI]
    public bool TryGetBatteryFromSlotOrEntity(Entity<PowerCellSlotComponent?> ent, [NotNullWhen(true)] out Entity<BatteryComponent>? battery)
    {
        if (TryGetBatteryFromSlot(ent, out battery))
            return true;

        if (TryComp<BatteryComponent>(ent, out var batteryComp))
        {
            battery = (ent.Owner, batteryComp);
            return true;
        }

        battery = null;
        return false;
    }

    /// <summary>
    /// First checks if the entity itself is a battery with <see cref="BatteryComponent"/>.
    /// If that fails it will try to get a battery from the entity's power cell slot instead.
    /// </summary>
    [PublicAPI]
    public bool TryGetBatteryFromEntityOrSlot(Entity<PowerCellSlotComponent?> ent, [NotNullWhen(true)] out Entity<BatteryComponent>? battery)
    {
        if (TryComp<BatteryComponent>(ent, out var batteryComp))
        {
            battery = (ent.Owner, batteryComp);
            return true;
        }
        if (TryGetBatteryFromSlot(ent, out battery))
            return true;

        battery = null;
        return false;
    }

    /// <summary>
    /// Tries to eject the power cell battery inside a power cell slot.
    /// This checks if the user has a free hand to do the ejection and if the slot is locked.
    /// </summary>
    /// <param name="ent">The entity with the power cell slot.</param>
    /// <param name="battery">The power cell that was ejected.</param>
    /// <param name="user">The player trying to eject the power cell from the slot.</param>
    /// <returns>If a power cell was sucessfully ejected.</returns>
    [PublicAPI]
    public bool TryEjectBatteryFromSlot(
        Entity<PowerCellSlotComponent?> ent,
        [NotNullWhen(true)] out EntityUid? battery,
        EntityUid? user = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
        {
            battery = null;
            return false;
        }

        if (!_itemSlots.TryEject(ent.Owner, ent.Comp.CellSlotId, user, out battery, excludeUserAudio: true))
        {
            battery = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns whether the entity has a slotted battery and charge for the requested action.
    /// </summary>
    /// <param name="ent">The entity with the power cell slot.</param>
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
    /// <param name="ent">The entity with the power cell slot.</param>
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
    /// <param name="ent">The entity with the power cell slot.</param>
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
    /// <param name="ent">The entity with the power cell slot.</param>
    /// <param name="cost">The cost per use.</param>
    [PublicAPI]
    public int GetMaxUses(Entity<PowerCellSlotComponent?> ent, float cost)
    {
        if (!TryGetBatteryFromSlot(ent, out var battery))
            return 0;

        return _battery.GetMaxUses(battery.Value.AsNullable(), cost);
    }
}
