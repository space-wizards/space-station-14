using System.Diagnostics.CodeAnalysis;
using Content.Shared.Database;

namespace Content.Shared.Containers.ItemSlots;

public sealed partial class ItemSlotsSystem
{
    /// <summary>
    /// Checks whether an item can currently be ejected from a slot.
    /// </summary>
    /// <remarks>
    /// Validation may raise <see cref="ItemSlotEjectAttemptEvent"/> on both the slot owner and the contained item,
    /// so callers must not treat this as a pure check. If <paramref name="popup"/> is provided, a locked slot may
    /// also show its configured failure popup to that entity.
    /// </remarks>
    public bool CanEject(EntityUid uid, ItemSlot slot, EntityUid? user, EntityUid? popup = null)
    {
        if (slot.Locked)
        {
            if (popup.HasValue && slot.LockedFailPopup.HasValue)
                _popupSystem.PopupEntity(Loc.GetString(slot.LockedFailPopup), uid, popup.Value);
            return false;
        }

        if (slot.ContainerSlot?.ContainedEntity is not { } item)
            return false;

        var ev = new ItemSlotEjectAttemptEvent(uid, item, user, slot);
        RaiseLocalEvent(uid, ref ev);
        RaiseLocalEvent(item, ref ev);
        if (ev.Cancelled)
            return false;

        return _containers.CanRemove(item, slot.ContainerSlot);
    }

    /// <summary>
    /// Ejects an item without performing validation. Returns false without producing success effects if the backing
    /// container does not remove the item.
    /// </summary>
    private bool Eject(EntityUid uid, ItemSlot slot, EntityUid item, EntityUid? user, bool excludeUserAudio = false)
    {
        if (slot.ContainerSlot == null || !_containers.Remove(item, slot.ContainerSlot))
            return false;

        if (user != null)
        {
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(user.Value)} ejected {ToPrettyString(item)} from {slot.ContainerSlot?.ID + " slot of "}{ToPrettyString(uid)}");
        }

        if (TryComp(uid, out ItemSlotsComponent? itemSlots))
            UpdateAppearance((uid, itemSlots));

        _audioSystem.PlayPredicted(slot.EjectSound, uid, excludeUserAudio ? user : null);
        return true;
    }

    /// <summary>
    /// Tries to eject an item from a slot.
    /// </summary>
    /// <remarks>
    /// If <paramref name="user"/> is provided, the user's pickup action blocker must also allow the item to be
    /// picked up.
    /// </remarks>
    /// <returns>True only if validation succeeds and the item was ejected.</returns>
    public bool TryEject(EntityUid uid,
        ItemSlot slot,
        EntityUid? user,
        [NotNullWhen(true)] out EntityUid? item,
        bool excludeUserAudio = false)
    {
        item = null;

        if (!CanEject(uid, slot, user))
            return false;

        item = slot.Item;

        if (user != null && item != null && !_actionBlockerSystem.CanPickup(user.Value, item.Value, showPopup: true))
            return false;

        return Eject(uid, slot, item!.Value, user, excludeUserAudio);
    }

    /// <summary>
    /// Tries to eject an item from a slot selected by ID.
    /// </summary>
    /// <returns>True only if the slot exists and the item was ejected.</returns>
    public bool TryEject(Entity<ItemSlotsComponent?> ent,
        string id,
        EntityUid? user,
        [NotNullWhen(true)] out EntityUid? item,
        bool excludeUserAudio = false)
    {
        item = null;

        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.Slots.TryGetValue(id, out var slot))
            return false;

        return TryEject(ent, slot, user, out item, excludeUserAudio);
    }

    /// <summary>
    /// Tries to eject an item and then place it in a user's hands or drop it.
    /// </summary>
    /// <returns>True if the item was ejected, even if it could not be placed in a hand.</returns>
    public bool TryEjectToHands(EntityUid uid, ItemSlot slot, EntityUid? user, bool excludeUserAudio = false)
    {
        if (!TryEject(uid, slot, user, out var item, excludeUserAudio))
            return false;

        if (user != null)
            _handsSystem.PickupOrDrop(user.Value, item.Value);

        return true;
    }

    /// <summary>
    /// Unlocks every occupied slot and attempts to eject its item.
    /// </summary>
    public void EjectFromAllSlots(Entity<ItemSlotsComponent> entity)
    {
        EjectFromAllSlots(entity, _ => true);
    }

    /// <summary>
    /// Unlocks matching occupied slots and attempts to eject their items on the floor.
    /// </summary>
    private void EjectFromAllSlots(Entity<ItemSlotsComponent> entity, Func<ItemSlot, bool> shouldEject)
    {
        foreach (var slot in entity.Comp.Slots.Values)
        {
            if (slot.HasItem && shouldEject(slot))
            {
                SetLock((entity.Owner, entity.Comp), slot, false);
                TryEject(entity.Owner, slot, null, out _);
            }
        }
    }
}
