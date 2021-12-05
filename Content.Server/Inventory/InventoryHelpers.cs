using Content.Server.Inventory.Components;
using Content.Server.Items;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using static Content.Shared.Inventory.EquipmentSlotDefines;

namespace Content.Server.Inventory
{
    public static class InventoryHelpers
    {
        public static bool SpawnItemInSlot(this InventoryComponent inventory, Slots slot, string prototype, bool mobCheck = false)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var protoManager = IoCManager.Resolve<IPrototypeManager>();
            var user = inventory.Owner;

            // Let's do nothing if the owner of the inventory has been deleted.
            if ((!IoCManager.Resolve<IEntityManager>().EntityExists(user) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(user).EntityLifeStage) >= EntityLifeStage.Deleted)
                return false;

            // If we don't have that slot or there's already an item there, we do nothing.
            if (!inventory.HasSlot(slot) || inventory.TryGetSlotItem(slot, out ItemComponent? _))
                return false;

            // If the prototype in question doesn't exist, we do nothing.
            if (!protoManager.HasIndex<EntityPrototype>(prototype))
                return false;

            // Let's spawn this first...
            var item = entityManager.SpawnEntity(prototype, IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(user).MapPosition);

            // Helper method that deletes the item and returns false.
            bool DeleteItem()
            {
                IoCManager.Resolve<IEntityManager>().DeleteEntity(item);
                return false;
            }

            // If this doesn't have an item component, then we can't do anything with it.
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(item, out ItemComponent? itemComp))
                return DeleteItem();

            // We finally try to equip the item, otherwise we delete it.
            return inventory.Equip(slot, itemComp, mobCheck) || DeleteItem();
        }
    }
}
