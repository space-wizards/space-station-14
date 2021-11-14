using Content.Server.Inventory.Components;
using Content.Server.Items;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using static Content.Shared.Inventory.EquipmentSlotDefines;

namespace Content.Server.Inventory
{
    public static class InventoryHelpers
    {
        public static bool SpawnItemInSlot(this InventoryComponent inventory, Slots slot, string prototype, bool mobCheck = false)
        {
            var entityManager = inventory.Owner.EntityManager;
            var protoManager = IoCManager.Resolve<IPrototypeManager>();
            var user = inventory.Owner;

            // Let's do nothing if the owner of the inventory has been deleted.
            if (user.Deleted)
                return false;

            // If we don't have that slot or there's already an item there, we do nothing.
            if (!inventory.HasSlot(slot) || inventory.TryGetSlotItem(slot, out ItemComponent? _))
                return false;

            // If the prototype in question doesn't exist, we do nothing.
            if (!protoManager.HasIndex<EntityPrototype>(prototype))
                return false;

            // Let's spawn this first...
            var item = entityManager.SpawnEntity(prototype, user.Transform.MapPosition);

            // Helper method that deletes the item and returns false.
            bool DeleteItem()
            {
                item.Delete();
                return false;
            }

            // If this doesn't have an item component, then we can't do anything with it.
            if (!item.TryGetComponent(out ItemComponent? itemComp))
                return DeleteItem();

            // We finally try to equip the item, otherwise we delete it.
            return inventory.Equip(slot, itemComp, mobCheck) || DeleteItem();
        }
    }
}
