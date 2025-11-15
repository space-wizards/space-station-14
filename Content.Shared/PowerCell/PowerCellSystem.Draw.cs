using Content.Shared.PowerCell.Components;
using JetBrains.Annotations;

namespace Content.Shared.PowerCell;

public sealed partial class PowerCellSystem
{
    /// <summary>
    /// Enables or disables the power cell draw.
    /// </summary>
    [PublicAPI]
    public void SetDrawEnabled(Entity<PowerCellDrawComponent?> ent, bool enabled)
    {
        if (!Resolve(ent, ref ent.Comp, false) || ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent, ent.Comp);

        if (TryGetBatteryFromSlot(ent.Owner, out var battery))
            _battery.RefreshChargeRate(battery.Value.AsNullable());
    }


    /// <summary>
    /// Returns whether the entity has a slotted battery and <see cref="PowerCellDrawComponent.UseCharge"/> charge.
    /// </summary>
    /// <param name="ent">The device with the power cell slot.</param>
    /// <param name="user">Show a popup to this user with the relevant details if specified.</param>
    /// <param name="user">Whether to predict the popup or not.</param>
    [PublicAPI]
    public bool HasActivatableCharge(Entity<PowerCellDrawComponent?, PowerCellSlotComponent?> ent, EntityUid? user = null, bool predicted = false)
    {
        // Default to true if we don't have the components.
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return true;

        return HasCharge((ent, ent.Comp2), ent.Comp1.UseCharge, user, predicted);
    }

    /// <summary>
    /// Tries to use the <see cref="PowerCellDrawComponent.UseCharge"/> for this entity.
    /// </summary>
    /// <param name="ent">The device with the power cell slot.</param>
    /// <param name="user">Show a popup to this user with the relevant details if specified.</param>
    /// <param name="user">Whether to predict the popup or not.</param>
    [PublicAPI]
    public bool TryUseActivatableCharge(Entity<PowerCellDrawComponent?, PowerCellSlotComponent?> ent, EntityUid? user = null, bool predicted = false)
    {
        // Default to true if we don't have the components.
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return true;

        if (TryUseCharge((ent, ent.Comp2), ent.Comp1.UseCharge, user, predicted))
            return true;

        return false;
    }

    /// <summary>
    /// Whether the power cell has any power at all for the draw rate.
    /// </summary>
    /// <param name="ent">The device with the power cell slot.</param>
    /// <param name="user">Show a popup to this user with the relevant details if specified.</param>
    /// <param name="user">Whether to predict the popup or not.</param>
    [PublicAPI]
    public bool HasDrawCharge(Entity<PowerCellDrawComponent?, PowerCellSlotComponent?> ent, EntityUid? user = null, bool predicted = false)
    {
        // Default to true if we don't have the components.
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2, false))
            return true;

        // 1 second of charge at the required draw rate.
        return HasCharge((ent, ent.Comp2), ent.Comp1.DrawRate, user, predicted);
    }
}
