using Content.Shared.Lock;

namespace Content.Shared.Containers.ItemSlots;

public sealed partial class ItemSlotsSystem
{
    [SubscribeLocalEvent]
    private void OnLockMapInit(Entity<ItemSlotsLockComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp(ent.Owner, out LockComponent? lockComp))
            return;

        UpdateLocks(ent, lockComp.Locked);
    }

    [SubscribeLocalEvent]
    private void OnLockToggled(Entity<ItemSlotsLockComponent> ent, ref LockToggledEvent args)
    {
        UpdateLocks(ent, args.Locked);
    }

    private void UpdateLocks(Entity<ItemSlotsLockComponent> ent, bool value)
    {
        foreach (var slot in ent.Comp.Slots)
        {
            if (!TryGetSlot(ent.Owner, slot, out var itemSlot))
                continue;

            SetLock(ent.Owner, itemSlot, value);
        }
    }

    /// <summary>
    /// Lock an item slot. This stops items from being inserted into or ejected from this slot.
    /// </summary>
    public void SetLock(EntityUid uid, string id, bool locked, ItemSlotsComponent? itemSlots = null)
    {
        if (!Resolve(uid, ref itemSlots))
            return;

        if (!itemSlots.Slots.TryGetValue(id, out var slot))
            return;

        SetLock(uid, slot, locked, itemSlots);
    }

    /// <summary>
    /// Lock an item slot. This stops items from being inserted into or ejected from this slot.
    /// </summary>
    public void SetLock(EntityUid uid, ItemSlot slot, bool locked, ItemSlotsComponent? itemSlots = null)
    {
        if (!Resolve(uid, ref itemSlots))
            return;

        slot.Locked = locked;
        Dirty(uid, itemSlots);
    }
}
