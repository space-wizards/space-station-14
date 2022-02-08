using System.Collections.Generic;
using Content.Shared.Inventory;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using InventoryComponent = Content.Shared.Inventory.InventoryComponent;

namespace Content.Server.AI.WorldState.States.Clothing
{
    [UsedImplicitly]
    public sealed class EquippedClothingState : StateData<Dictionary<string, EntityUid>>
    {
        public override string Name => "EquippedClothing";

        public override Dictionary<string, EntityUid> GetValue()
        {
            var result = new Dictionary<string, EntityUid>();

            var invSystem = EntitySystem.Get<InventorySystem>();
            if (!invSystem.TryGetSlots(Owner, out var slotDefinitions))
            {
                return result;
            }

            foreach (var slot in slotDefinitions)
            {
                if (!invSystem.HasSlot(Owner, slot.Name)) continue;

                if (invSystem.TryGetSlotEntity(Owner, slot.Name, out var entityUid))
                {
                    result.Add(slot.Name, entityUid.Value);
                }
            }

            return result;
        }
    }
}
