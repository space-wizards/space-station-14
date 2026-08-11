using System.Diagnostics.CodeAnalysis;
using Content.Shared.Database;

namespace Content.Shared.Containers.ItemSlots;

public sealed partial class ItemSlotsSystem
{
    /// <summary>
    /// Check whether an ejection from a given slot may happen.
    /// </summary>
    /// <remarks>
    /// If a popup entity is given, this will generate a popup message if any are configured on the item slot.
    /// </remarks>
    public bool CanEject(EntityUid uid, EntityUid? user, ItemSlot slot, EntityUid? popup = null)
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
    /// Eject an item from a slot. This does not perform checks (e.g., is the slot locked?), so you should
    /// probably just use <see cref="TryEject"/> instead.
    /// </summary>
    /// <param name="excludeUserAudio">If true, will exclude the user when playing sound. Does nothing client-side.
    /// Useful for predicted interactions</param>
    private void Eject(EntityUid uid, ItemSlot slot, EntityUid item, EntityUid? user, bool excludeUserAudio = false)
    {
        bool? ejected = slot.ContainerSlot != null ? _containers.Remove(item, slot.ContainerSlot) : null;

        if (ejected != null && ejected.Value && user != null)
        {
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(user.Value)} ejected {ToPrettyString(item)} from {slot.ContainerSlot?.ID + " slot of "}{ToPrettyString(uid)}");
        }

        _audioSystem.PlayPredicted(slot.EjectSound, uid, excludeUserAudio ? user : null);
    }

    /// <summary>
    /// Try to eject an item from a slot.
    /// </summary>
    /// <returns>False if item slot is locked or has no item inserted</returns>
    public bool TryEject(EntityUid uid,
        ItemSlot slot,
        EntityUid? user,
        [NotNullWhen(true)] out EntityUid? item,
        bool excludeUserAudio = false)
    {
        item = null;

        if (!CanEject(uid, user, slot))
            return false;

        item = slot.Item;

        if (user != null && item != null && !_actionBlockerSystem.CanPickup(user.Value, item.Value, showPopup: true))
            return false;

        Eject(uid, slot, item!.Value, user, excludeUserAudio);
        return true;
    }

    /// <summary>
    /// Try to eject item from a slot.
    /// </summary>
    /// <returns>False if the id is not valid, the item slot is locked, or it has no item inserted</returns>
    public bool TryEject(EntityUid uid,
        string id,
        EntityUid? user,
        [NotNullWhen(true)] out EntityUid? item,
        ItemSlotsComponent? itemSlots = null,
        bool excludeUserAudio = false)
    {
        item = null;

        if (!Resolve(uid, ref itemSlots))
            return false;

        if (!itemSlots.Slots.TryGetValue(id, out var slot))
            return false;

        return TryEject(uid, slot, user, out item, excludeUserAudio);
    }

    /// <summary>
    /// Try to eject item from a slot directly into a user's hands. If they have no hands, the item will still
    /// be ejected onto the floor.
    /// </summary>
    /// <returns>
    /// False if the id is not valid, the item slot is locked, or it has no item inserted. True otherwise, even
    /// if the user has no hands.
    /// </returns>
    public bool TryEjectToHands(EntityUid uid, ItemSlot slot, EntityUid? user, bool excludeUserAudio = false)
    {
        if (!TryEject(uid, slot, user, out var item, excludeUserAudio))
            return false;

        if (user != null)
            _handsSystem.PickupOrDrop(user.Value, item.Value);

        return true;
    }

    /// <summary>
    /// Unlocks all slots and ejects items from them on the floor.
    /// </summary>
    public void EjectFromAllSlots(Entity<ItemSlotsComponent> entity)
    {
        EjectFromAllSlots(entity, _ => true);
    }

    /// <summary>
    /// Unlocks all slots and ejects items from them on the floor while <paramref name="shouldEject"/> returns true.
    /// </summary>
    private void EjectFromAllSlots(Entity<ItemSlotsComponent> entity, Func<ItemSlot, bool> shouldEject)
    {
        foreach (var slot in entity.Comp.Slots.Values)
        {
            if (slot.HasItem && shouldEject(slot))
            {
                SetLock(entity.Owner, slot, false, entity.Comp);
                TryEject(entity.Owner, slot, null, out _);
            }
        }
    }
}
