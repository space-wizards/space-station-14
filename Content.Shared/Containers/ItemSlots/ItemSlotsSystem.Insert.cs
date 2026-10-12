using System.Diagnostics.CodeAnalysis;
using Content.Shared.Database;
using Content.Shared.Hands.Components;

namespace Content.Shared.Containers.ItemSlots;

public sealed partial class ItemSlotsSystem
{
    /// <summary>
    /// Inserts an item without performing validation. Returns false without producing success effects if the
    /// backing container rejects the item.
    /// </summary>
    private bool Insert(EntityUid uid,
        ItemSlot slot,
        EntityUid item,
        EntityUid? user,
        bool excludeUserAudio = false)
    {
        if (slot.ContainerSlot == null || !_containers.Insert(item, slot.ContainerSlot))
            return false;

        if (user != null)
        {
            _adminLogger.Add(LogType.Action,
                LogImpact.Low,
                $"{ToPrettyString(user.Value)} inserted {ToPrettyString(item)} into {slot.ContainerSlot?.ID + " slot of "}{ToPrettyString(uid)}");
        }

        _audioSystem.PlayPredicted(slot.InsertSound, uid, excludeUserAudio ? user : null);
        return true;
    }

    /// <summary>
    /// Checks whether an item can currently be inserted into a slot. Unless <paramref name="swap"/> is true, this
    /// returns false if the slot is occupied.
    /// </summary>
    /// <remarks>
    /// Validation may raise <see cref="ItemSlotInsertAttemptEvent"/> on both the slot owner and the candidate item,
    /// so callers must not treat this as a pure check.
    /// </remarks>
    public bool CanInsert(EntityUid uid,
        ItemSlot slot,
        EntityUid item,
        EntityUid? user,
        bool swap = false)
    {
        if (slot.ContainerSlot == null)
            return false;

        if (slot.HasItem && (!swap || swap && !CanEject(uid, slot, user)))
            return false;

        if (!CanInsertWhitelist(item, slot))
            return false;

        if (slot.Locked)
            return false;

        var ev = new ItemSlotInsertAttemptEvent(uid, item, user, slot);
        RaiseLocalEvent(uid, ref ev);
        RaiseLocalEvent(item, ref ev);
        if (ev.Cancelled)
            return false;

        return _containers.CanInsert(item, slot.ContainerSlot, assumeEmpty: swap);
    }

    private bool CanInsertWhitelist(EntityUid item, ItemSlot slot)
    {
        if (_whitelistSystem.IsWhitelistFail(slot.Whitelist, item)
            || _whitelistSystem.IsWhitelistPass(slot.Blacklist, item))
            return false;
        return true;
    }

    /// <summary>
    /// Tries to insert an item into a slot selected by ID.
    /// </summary>
    /// <returns>True only if the slot exists and the item was inserted.</returns>
    public bool TryInsert(Entity<ItemSlotsComponent?> ent,
        string id,
        EntityUid item,
        EntityUid? user,
        bool excludeUserAudio = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.Slots.TryGetValue(id, out var slot))
            return false;

        return TryInsert(ent, slot, item, user, excludeUserAudio: excludeUserAudio);
    }

    /// <summary>
    /// Tries to insert an item into a specific slot.
    /// </summary>
    /// <returns>True only if validation succeeds and the item was inserted.</returns>
    public bool TryInsert(EntityUid uid,
        ItemSlot slot,
        EntityUid item,
        EntityUid? user,
        bool excludeUserAudio = false)
    {
        if (!CanInsert(uid, slot, item, user))
            return false;

        return Insert(uid, slot, item, user, excludeUserAudio: excludeUserAudio);
    }

    /// <summary>
    /// Tries to insert the item in a user's active hand into a specific slot.
    /// </summary>
    /// <remarks>
    /// If insertion fails after the item is dropped, the item remains dropped.
    /// </remarks>
    /// <returns>True only if the held item was dropped and inserted.</returns>
    public bool TryInsertFromHand(EntityUid uid,
        ItemSlot slot,
        Entity<HandsComponent?> user,
        bool excludeUserAudio = false)
    {
        if (!Resolve(user, ref user.Comp, false))
            return false;

        if (!_handsSystem.TryGetActiveItem(user, out var held))
            return false;

        if (!CanInsert(uid, slot, held.Value, user))
            return false;

        if (!_handsSystem.TryDrop(user, user.Comp.ActiveHandId!))
            return false;

        return Insert(uid, slot, held.Value, user, excludeUserAudio: excludeUserAudio);
    }

    /// <summary>
    /// Tries to insert an item into any compatible empty slot.
    /// </summary>
    /// <param name="ent">The entity that has the item slots.</param>
    /// <param name="item">The item to be inserted.</param>
    /// <param name="user">The entity performing the interaction.</param>
    /// <param name="excludeUserAudio">
    /// If true, will exclude the user when playing sound. Does nothing client-side.
    /// Useful for predicted interactions.
    /// </param>
    /// <returns>True only if a compatible slot was found and the item was inserted.</returns>
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

        return Insert(ent, itemSlot, item, user, excludeUserAudio: excludeUserAudio);
    }

    /// <summary>
    /// Tries to get a slot that the <paramref name="item"/> can be inserted into.
    /// </summary>
    /// <param name="ent">Entity that <paramref name="item"/> is being inserted into.</param>
    /// <param name="item">Entity being inserted into <paramref name="ent"/>.</param>
    /// <param name="userEnt">Entity inserting <paramref name="item"/> into <paramref name="ent"/>.</param>
    /// <param name="itemSlot">The ItemSlot on <paramref name="ent"/> to insert <paramref name="item"/> into.</param>
    /// <param name="emptyOnly">If true, occupied slots are skipped before validation.</param>
    /// <returns>True when a compatible slot is found. Otherwise, false.</returns>
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

            if (CanInsert(ent, slot, item, userEnt))
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
        var aEmpty = !a.HasItem;
        var bEmpty = !b.HasItem;

        if (aEmpty != bEmpty)
            return aEmpty ? -1 : 1;

        return a.Priority.CompareTo(b.Priority);
    }
}
