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

    private void UpdateLocks(Entity<ItemSlotsLockComponent> ent, bool locked)
    {
        foreach (var slot in ent.Comp.Slots)
        {
            if (!TryGetSlot(ent.Owner, slot, out var itemSlot))
                continue;

            SetLock(ent.Owner, itemSlot, locked);
        }
    }

    /// <summary>
    /// Sets whether an item slot is locked, preventing checked insertion and ejection while locked.
    /// </summary>
    public void SetLock(Entity<ItemSlotsComponent?> ent, string id, bool locked)
    {
        if (!_itemSlotsQuery.Resolve(ent, ref ent.Comp))
            return;

        if (!ent.Comp.Slots.TryGetValue(id, out var slot))
            return;

        SetLock(ent, slot, locked);
    }

    /// <summary>
    /// Sets whether an item slot is locked, preventing checked insertion and ejection while locked.
    /// </summary>
    public void SetLock(Entity<ItemSlotsComponent?> ent, ItemSlot slot, bool locked)
    {
        if (!_itemSlotsQuery.Resolve(ent, ref ent.Comp))
            return;

        slot.Locked = locked;
        Dirty(ent);
    }
}
