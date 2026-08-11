using System.Diagnostics.CodeAnalysis;
using Content.Shared.Database;
using Content.Shared.Hands.Components;

namespace Content.Shared.Containers.ItemSlots;

public sealed partial class ItemSlotsSystem
{
    /// <summary>
    /// Insert an item into a slot. This does not perform checks, so make sure to also use <see
    /// cref="CanInsert"/> or just use <see cref="TryInsert"/> instead.
    /// </summary>
    /// <param name="excludeUserAudio">If true, will exclude the user when playing sound. Does nothing client-side.
    /// Useful for predicted interactions</param>
    private void Insert(EntityUid uid,
        ItemSlot slot,
        EntityUid item,
        EntityUid? user,
        bool excludeUserAudio = false)
    {
        bool? inserted = slot.ContainerSlot != null ? _containers.Insert(item, slot.ContainerSlot) : null;

        if (inserted != null && inserted.Value && user != null)
        {
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(user.Value)} inserted {ToPrettyString(item)} into {slot.ContainerSlot?.ID + " slot of "}{ToPrettyString(uid)}");
        }

        _audioSystem.PlayPredicted(slot.InsertSound, uid, excludeUserAudio ? user : null);
    }

    /// <summary>
    /// Check whether a given item can be inserted into a slot. Unless otherwise specified, this will return
    /// false if the slot is already filled.
    /// </summary>
    public bool CanInsert(EntityUid uid,
        EntityUid usedUid,
        EntityUid? user,
        ItemSlot slot,
        bool swap = false)
    {
        if (slot.ContainerSlot == null)
            return false;

        if (slot.HasItem && (!swap || swap && !CanEject(uid, user, slot)))
            return false;

        if (!CanInsertWhitelist(usedUid, slot))
            return false;

        if (slot.Locked)
            return false;

        var ev = new ItemSlotInsertAttemptEvent(uid, usedUid, user, slot);
        RaiseLocalEvent(uid, ref ev);
        RaiseLocalEvent(usedUid, ref ev);
        if (ev.Cancelled)
            return false;

        return _containers.CanInsert(usedUid, slot.ContainerSlot, assumeEmpty: swap);
    }

    private bool CanInsertWhitelist(EntityUid usedUid, ItemSlot slot)
    {
        if (_whitelistSystem.IsWhitelistFail(slot.Whitelist, usedUid)
            || _whitelistSystem.IsWhitelistPass(slot.Blacklist, usedUid))
            return false;
        return true;
    }

    /// <summary>
    /// Tries to insert item into a specific slot.
    /// </summary>
    /// <returns>False if failed to insert item</returns>
    public bool TryInsert(EntityUid uid,
        string id,
        EntityUid item,
        EntityUid? user,
        ItemSlotsComponent? itemSlots = null,
        bool excludeUserAudio = false)
    {
        if (!Resolve(uid, ref itemSlots))
            return false;

        if (!itemSlots.Slots.TryGetValue(id, out var slot))
            return false;

        return TryInsert(uid, slot, item, user, excludeUserAudio: excludeUserAudio);
    }

    /// <summary>
    /// Tries to insert item into a specific slot.
    /// </summary>
    /// <returns>False if failed to insert item</returns>
    public bool TryInsert(EntityUid uid,
        ItemSlot slot,
        EntityUid item,
        EntityUid? user,
        bool excludeUserAudio = false)
    {
        if (!CanInsert(uid, item, user, slot))
            return false;

        Insert(uid, slot, item, user, excludeUserAudio: excludeUserAudio);
        return true;
    }

    /// <summary>
    /// Tries to insert item into a specific slot from an entity's hand.
    /// Does not check action blockers.
    /// </summary>
    /// <returns>False if failed to insert item</returns>
    public bool TryInsertFromHand(EntityUid uid,
        ItemSlot slot,
        EntityUid user,
        HandsComponent? hands = null,
        bool excludeUserAudio = false)
    {
        if (!Resolve(user, ref hands, false))
            return false;

        if (!_handsSystem.TryGetActiveItem((user, hands), out var held))
            return false;

        if (!CanInsert(uid, held.Value, user, slot))
            return false;

        if (!_handsSystem.TryDrop(user, hands.ActiveHandId!))
            return false;

        Insert(uid, slot, held.Value, user, excludeUserAudio: excludeUserAudio);
        return true;
    }

    /// <summary>
    /// Tries to insert an item into any empty slot.
    /// </summary>
    /// <param name="ent">The entity that has the item slots.</param>
    /// <param name="item">The item to be inserted.</param>
    /// <param name="user">The entity performing the interaction.</param>
    /// <param name="excludeUserAudio">
    /// If true, will exclude the user when playing sound. Does nothing client-side.
    /// Useful for predicted interactions
    /// </param>
    /// <returns>False if failed to insert item</returns>
    public bool TryInsertEmpty(Entity<ItemSlotsComponent?> ent,
        EntityUid item,
        EntityUid? user,
        bool excludeUserAudio = false)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!TryGetAvailableSlot(ent,
                item,
                user,
                out var itemSlot,
                emptyOnly: true))
            return false;

        if (user != null && !_handsSystem.TryDrop(user.Value, item))
            return false;

        Insert(ent, itemSlot, item, user, excludeUserAudio: excludeUserAudio);
        return true;
    }

    /// <summary>
    /// Tries to get any slot that the <paramref name="item"/> can be inserted into.
    /// </summary>
    /// <param name="ent">Entity that <paramref name="item"/> is being inserted into.</param>
    /// <param name="item">Entity being inserted into <paramref name="ent"/>.</param>
    /// <param name="userEnt">Entity inserting <paramref name="item"/> into <paramref name="ent"/>.</param>
    /// <param name="itemSlot">The ItemSlot on <paramref name="ent"/> to insert <paramref name="item"/> into.</param>
    /// <param name="emptyOnly">True only returns slots that are empty.
    /// False returns any slot that is able to receive <paramref name="item"/>.</param>
    /// <returns>True when a slot is found. Otherwise, false.</returns>
    public bool TryGetAvailableSlot(Entity<ItemSlotsComponent?> ent,
        EntityUid item,
        Entity<HandsComponent?>? userEnt,
        [NotNullWhen(true)] out ItemSlot? itemSlot,
        bool emptyOnly = false)
    {
        itemSlot = null;

        if (userEnt is { } user
            && Resolve(user, ref user.Comp)
            && _handsSystem.IsHolding(user, item))
        {
            if (!_handsSystem.CanDrop(user, item))
                return false;
        }

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        var slots = new List<ItemSlot>();
        foreach (var slot in ent.Comp.Slots.Values)
        {
            if (emptyOnly && slot.ContainerSlot?.ContainedEntity != null)
                continue;

            if (CanInsert(ent, item, userEnt, slot))
                slots.Add(slot);
        }

        if (slots.Count == 0)
            return false;

        slots.Sort(SortEmpty);

        itemSlot = slots[0];
        return true;
    }

    private static int SortEmpty(ItemSlot a, ItemSlot b)
    {
        var aEnt = a.ContainerSlot?.ContainedEntity;
        var bEnt = b.ContainerSlot?.ContainedEntity;
        if (aEnt == null && bEnt == null)
            return a.Priority.CompareTo(b.Priority);

        if (aEnt == null)
            return -1;

        return 1;
    }
}
