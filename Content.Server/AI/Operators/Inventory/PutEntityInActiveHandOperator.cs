using Content.Server.GameObjects;
using Content.Shared.GameObjects.Components.Inventory;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.HTN.Tasks.Primitive.Operators.Inventory
{
    public class PutEntityInActiveHandOperator : IOperator
    {
        // Input variables
        private readonly IEntity _owner;
        private readonly IEntity _target;

        public PutEntityInActiveHandOperator(IEntity owner, IEntity target)
        {
            _owner = owner;
            _target = target;
        }

        public Outcome Execute(float frameTime)
        {
            _owner.TryGetComponent(out InventoryComponent inventory);
            // First get clear hand
            _owner.TryGetComponent(out HandsComponent hands);

            ItemComponent item = null;
            EquipmentSlotDefines.Slots itemSlot = EquipmentSlotDefines.Slots.NONE;
            bool found = false;

            foreach (var slot in EquipmentSlotDefines.SlotMasks.Keys)
            {
                if (!inventory.TryGetSlotItem(slot, out ItemComponent foundItem) || foundItem.Owner != _target) continue;
                item = foundItem;
                itemSlot = slot;
                found = true;
                break;
            }

            if (!found)
            {
                return Outcome.Failed;
            }

            if (!hands.CanPutInHand(item)) return Outcome.Failed;
            inventory.Unequip(itemSlot);
            hands.PutInHand(item);
            hands.ActiveIndex = hands.FindHand(item.Owner);
            return Outcome.Success;

        }
    }
}
