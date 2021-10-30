using System.Collections.Generic;
using Content.Server.Inventory.Components;
using Content.Shared.Inventory;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.WorldState.States.Clothing
{
    [UsedImplicitly]
    public sealed class EquippedClothingState : StateData<Dictionary<EquipmentSlotDefines.Slots, IEntity>>
    {
        public override string Name => "EquippedClothing";

        public override Dictionary<EquipmentSlotDefines.Slots, IEntity> GetValue()
        {
            var result = new Dictionary<EquipmentSlotDefines.Slots, IEntity>();

            if (!Owner.TryGetComponent(out InventoryComponent? inventoryComponent))
            {
                return result;
            }

            foreach (var slot in EquipmentSlotDefines.AllSlots)
            {
                if (!inventoryComponent.HasSlot(slot)) continue;
                var slotItem = inventoryComponent.GetSlotItem(slot);

                if (slotItem != null)
                {
                    result.Add(slot, slotItem.Owner);
                }
            }

            return result;
        }
    }
}
