using Content.Shared.Clothing.Components;
using Content.Shared.Item;
using Robust.Shared.Prototypes;

namespace Content.Shared.Inventory;

public partial class InventorySystem
{
    public bool SpawnItemInSlot(EntityUid uid, string slot, string prototype, bool silent = false, bool force = false, InventoryComponent? inventory = null)
    {
        if (!Resolve(uid, ref inventory, false))
            return false;

        // Let's do nothing if the owner of the inventory has been deleted.
        if (Deleted(uid))
            return false;

        // If we don't have that slot or there's already an item there, we do nothing.
        if (!HasSlot(uid, slot) || TryGetSlotEntity(uid, slot, out _, inventory))
            return false;

        // If the prototype in question doesn't exist, we do nothing.
        if (!_prototypeManager.HasIndex<EntityPrototype>(prototype))
            return false;

        // Let's spawn this first...
        var item = EntityManager.SpawnEntity(prototype, Transform(uid).Coordinates);

        // Helper method that deletes the item and returns false.
        bool DeleteItem()
        {
            EntityManager.DeleteEntity(item);
            return false;
        }

        // We finally try to equip the item, otherwise we delete it.
        return TryEquip(uid, item, slot, silent, force) || DeleteItem();
    }
}
