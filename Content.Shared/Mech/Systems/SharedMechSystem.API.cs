using JetBrains.Annotations;
using Content.Shared.Body.Events;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Mech.Module.Components;
using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Mech.Systems;

public abstract partial class SharedMechSystem
{
    /// <summary>
    /// Insert an equipment or module entity into the mech.
    /// </summary>
    /// <param name="ent">The mech.</param>
    /// <param name="toInsert">The equipment or module entity to insert.</param>
    /// <param name="equipmentComponent">Optional resolved equipment component for <paramref name="toInsert"/>.</param>
    /// <param name="moduleComponent">Optional resolved module component for <paramref name="toInsert"/>.</param>
    [PublicAPI]
    public void InsertEquipment(Entity<MechComponent?> ent,
        EntityUid toInsert,
        MechEquipmentComponent? equipmentComponent = null,
        MechModuleComponent? moduleComponent = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.Broken)
            return;

        // Equipment
        if (Resolve(toInsert, ref equipmentComponent, false))
        {
            if (ent.Comp.EquipmentContainer.ContainedEntities.Count >= ent.Comp.MaxEquipmentAmount)
                return;

            if (_entWhitelist.IsWhitelistFail(ent.Comp.EquipmentWhitelist, toInsert))
                return;

            equipmentComponent.EquipmentOwner = ent.Owner;
            _container.Insert(toInsert, ent.Comp.EquipmentContainer);
            var ev = new MechEquipmentInsertedEvent(ent.Owner);
            RaiseLocalEvent(toInsert, ref ev);
            UpdateMechUi(ent.Owner);
            return;
        }

        // Module.
        if (Resolve(toInsert, ref moduleComponent, false))
        {
            if (ent.Comp.ModuleContainer.ContainedEntities.Count >= ent.Comp.MaxModuleAmount)
                return;

            if (_entWhitelist.IsWhitelistFail(ent.Comp.ModuleWhitelist, toInsert))
                return;

            moduleComponent.ModuleOwner = ent.Owner;
            _container.Insert(toInsert, ent.Comp.ModuleContainer);
            var modEv = new MechModuleInsertedEvent(ent.Owner);
            RaiseLocalEvent(toInsert, ref modEv);
            UpdateMechUi(ent.Owner);
        }
    }

    /// <summary>
    /// Update virtual hand items for the mech's pilot so they block hands based on
    /// the currently selected equipment (or the mech itself).
    /// </summary>
    [PublicAPI]
    public void RefreshPilotHandVirtualItems(Entity<MechComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var pilot = Vehicle.GetOperatorOrNull(ent.Owner);
        if (pilot == null)
            return;

        foreach (var held in _hands.EnumerateHeld(pilot.Value))
        {
            if (!TryComp<VirtualItemComponent>(held, out var virt))
                continue;

            var newBlocking = ent.Comp.CurrentSelectedEquipment ?? ent.Owner;
            if (virt.BlockingEntity == newBlocking)
                continue;

            virt.BlockingEntity = newBlocking;
            Dirty(held, virt);
        }
    }


    /// <summary>
    /// Sets the integrity of the mech.
    /// </summary>
    /// <param name="ent">The mech.</param>
    /// <param name="value">New integrity value (clamped to [0, MaxIntegrity]).</param>
    [PublicAPI]
    public void SetIntegrity(Entity<MechComponent?> ent, FixedPoint2 value)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Integrity = FixedPoint2.Clamp(value, 0, ent.Comp.MaxIntegrity);

        // Handle broken state transitions based on integrity
        if (ent.Comp.Integrity <= 0)
        {
            // If already in broken state, check if should be gibbed
            if (ent.Comp.Broken)
            {
                if (ent.Comp.Integrity < -ent.Comp.BrokenThreshold)
                {
                    var gibEvent = new BeingGibbedEvent([]);
                    RaiseLocalEvent(ent.Owner, ref gibEvent);
                    return;
                }
            }
            else
            {
                SetBrokenState(ent);
            }
        }
        else if (ent.Comp.Integrity > ent.Comp.BrokenThreshold && ent.Comp.Broken)
        {
            ent.Comp.Broken = false;
        }

        Dirty(ent);
        UpdateMechUi(ent.Owner);
        UpdateAppearance((ent.Owner, ent.Comp));
    }

    /// <summary>
    /// Attempt to change the mech's energy by draining charge from its slotted battery.
    /// </summary>
    /// <param name="ent">The mech.</param>
    /// <param name="delta">Amount to change energy by (negative to drain).</param>
    /// <returns>True if the requested energy was available and applied; false otherwise.</returns>
    [PublicAPI]
    public bool TryChangeEnergy(Entity<MechComponent?> ent, FixedPoint2 delta)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (delta > 0)
            return false;

        var amount = MathF.Abs(delta.Float());
        if (!_powerCell.TryUseCharge(ent.Owner, amount))
            return false;

        UpdateMechUi(ent.Owner);
        UpdateBatteryAlert(ent);

        return true;
    }

    /// <summary>
    /// Update battery alerts shown on the mech.
    /// </summary>
    [PublicAPI]
    public void UpdateBatteryAlert(Entity<MechComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out var cell))
        {
            _alerts.ClearAlert(ent.Owner, ent.Comp.BatteryAlert);
            _alerts.ShowAlert(ent.Owner, ent.Comp.NoBatteryAlert);
            return;
        }

        var charge = _battery.GetCharge(cell.Value.AsNullable());
        var maxCharge = cell.Value.Comp.MaxCharge;
        var chargePercent = (short)MathF.Round(charge / maxCharge * 10f);

        // we make sure 0 only shows if they have absolutely no battery.
        // also account for floating point imprecision
        if (chargePercent == 0 && charge > 0)
            chargePercent = 1;

        _alerts.ClearAlert(ent.Owner, ent.Comp.NoBatteryAlert);
        _alerts.ShowAlert(ent.Owner, ent.Comp.BatteryAlert, chargePercent);
    }

    /// <summary>
    /// Update health alerts shown on the mech.
    /// </summary>
    [PublicAPI]
    public void UpdateHealthAlert(Entity<MechComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.Broken)
        {
            // Mech is broken.
            _alerts.ClearAlert(ent.Owner, ent.Comp.HealthAlert);
            _alerts.ShowAlert(ent.Owner, ent.Comp.BrokenAlert);
        }
        else
        {
            // Mech is healthy, show health percentage.
            _alerts.ClearAlert(ent.Owner, ent.Comp.BrokenAlert);

            var integrity = ent.Comp.Integrity.Float();
            var maxIntegrity = ent.Comp.MaxIntegrity.Float();
            var healthPercent = (short)MathF.Round((1f - integrity / maxIntegrity) * 4f);
            _alerts.ShowAlert(ent.Owner, ent.Comp.HealthAlert, healthPercent);
        }
    }

    /// <summary>
    /// Eject the battery from the mech's battery slot.
    /// </summary>
    [PublicAPI]
    public void RemoveBattery(Entity<MechComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        _container.EmptyContainer(ent.Comp.BatterySlot);
        _actionBlocker.UpdateCanMove(ent.Owner);
        UpdateMechUi(ent.Owner);
        Dirty(ent);
    }

    /// <summary>
    /// Puts the mech into a broken state: ejects all contents, disables control, but allows repair.
    /// </summary>
    [PublicAPI]
    public void SetBrokenState(Entity<MechComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var pilot = ent.Comp.PilotSlot.ContainedEntity;

        if (pilot.HasValue)
            ManageVirtualItems((ent.Owner, ent.Comp), pilot.Value, create: false);

        // In broken state, equipment, modules, and battery are ejected.
        var equipment = new List<EntityUid>(ent.Comp.EquipmentContainer.ContainedEntities);
        foreach (var equipmentEnt in equipment)
        {
            _container.Remove(equipmentEnt, ent.Comp.EquipmentContainer);
            ScatterEntityFromMech(equipmentEnt);
        }

        var modules = new List<EntityUid>(ent.Comp.ModuleContainer.ContainedEntities);
        foreach (var modulesEnt in modules)
        {
            _container.Remove(modulesEnt, ent.Comp.ModuleContainer);
            ScatterEntityFromMech(modulesEnt);
        }

        if (ent.Comp.BatterySlot.ContainedEntity != null)
        {
            var battery = ent.Comp.BatterySlot.ContainedEntity.Value;

            // Remove from container and throw from mech position.
            _container.Remove(battery, ent.Comp.BatterySlot);
            ScatterEntityFromMech(battery);
        }

        // Eject pilot from the mech when entering broken state.
        if (pilot.HasValue)
        {
            TryEject(ent);
            ScatterEntityFromMech(pilot.Value);
        }

        ent.Comp.Broken = true;
        UpdateAppearance((ent.Owner, ent.Comp));
        Dirty(ent);
        UpdateMechUi(ent.Owner);

        // Play broken sound.
        if (ent.Comp.BrokenSound != null)
        {
            var ev = new MechBrokenSoundEvent(ent.Owner, ent.Comp.BrokenSound);
            RaiseLocalEvent(ent.Owner, ref ev);
        }
    }

    /// <summary>
    /// Repair a broken mech: restore integrity, clear broken flag and refresh appearance.
    /// </summary>
    [PublicAPI]
    public void RepairMech(Entity<MechComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!ent.Comp.Broken)
            return;

        // Restore integrity to a safe level above broken threshold.
        var repairAmount = ent.Comp.MaxIntegrity;
        SetIntegrity(ent, repairAmount);

        // Reset broken state.
        ent.Comp.Broken = false;

        UpdateAppearance((ent.Owner, ent.Comp));
        Dirty(ent);
        UpdateMechUi(ent.Owner);
    }

    /// <summary>
    /// Return whether <paramref name="toInsert"/> can be inserted as a pilot into this mech.
    /// Performs checks for broken state, existing operator, action-blockers and slot availability.
    /// </summary>
    /// <param name="ent">The mech.</param>
    /// <param name="toInsert">Candidate entity to insert.</param>
    /// <returns>True if insertion is allowed.</returns>
    [PublicAPI]
    public virtual bool CanInsert(Entity<MechComponent?> ent, EntityUid toInsert)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.Broken)
            return false;

        if (!_actionBlocker.CanMove(toInsert))
            return false;

        if (Vehicle.GetOperatorOrNull(ent.Owner) == toInsert)
            return false;

        if (!_container.CanInsert(toInsert, ent.Comp.PilotSlot))
            return false;

        return true;
    }

    /// <summary>
    /// Attempt to insert <paramref name="toInsert"/> into the mech's pilot slot.
    /// </summary>
    /// <param name="ent">The mech.</param>
    /// <param name="toInsert">Entity to insert as pilot.</param>
    /// <returns>True if insertion succeeded.</returns>
    [PublicAPI]
    public bool TryInsert(Entity<MechComponent?> ent, EntityUid toInsert)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!CanInsert(ent, toInsert))
            return false;

        _container.Insert(toInsert, ent.Comp.PilotSlot);
        return true;
    }

    /// <summary>
    /// Attempt to eject the current pilot from the mech.
    /// </summary>
    [PublicAPI]
    public bool TryEject(Entity<MechComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!Vehicle.TryGetOperator(ent.Owner, out var operatorEnt))
            return false;

        _container.RemoveEntity(ent.Owner, operatorEnt.Value);
        return true;
    }

    /// <summary>
    /// Updates the UI.
    /// </summary>
    [PublicAPI]
    public void UpdateMechUi(EntityUid uid)
    {
        var ev = new UpdateMechUiEvent();
        RaiseLocalEvent(uid, ev);
    }

    /// <summary>
    /// Raised when a mech enters broken state to play sound.
    /// </summary>
    [ByRefEvent]
    public record struct MechBrokenSoundEvent(EntityUid Mech, SoundSpecifier Sound);

    /// <summary>
    /// Raised when a pilot successfully enters a mech and an optional entry sound should be played.
    /// </summary>
    [ByRefEvent]
    public record struct MechEntrySuccessSoundEvent(EntityUid Mech, SoundSpecifier Sound);
}

/// <summary>
/// Event to request mech UI update.
/// </summary>
[Serializable, NetSerializable]
public sealed class UpdateMechUiEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed partial class RemoveBatteryEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class MechExitEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class MechEntryEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class RemoveModuleEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed class MechDnaLockRegisterEvent : EntityEventArgs
{
    public NetEntity User;
}

[Serializable, NetSerializable]
public sealed class MechDnaLockToggleEvent : EntityEventArgs
{
    public NetEntity User;
}

[Serializable, NetSerializable]
public sealed class MechDnaLockResetEvent : EntityEventArgs
{
    public NetEntity User;
}

[Serializable, NetSerializable]
public sealed class MechCardLockRegisterEvent : EntityEventArgs
{
    public NetEntity User;
}

[Serializable, NetSerializable]
public sealed class MechCardLockToggleEvent : EntityEventArgs
{
    public NetEntity User;
}

[Serializable, NetSerializable]
public sealed class MechCardLockResetEvent : EntityEventArgs
{
    public NetEntity User;
}

[Serializable, NetSerializable]
public sealed class MechDnaLockRegisterMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MechDnaLockToggleMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MechDnaLockResetMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MechCardLockRegisterMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MechCardLockToggleMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class MechCardLockResetMessage : BoundUserInterfaceMessage;
