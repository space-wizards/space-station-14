using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Clothing;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.Clothing.Components;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Considerations.Clothing
{
    public sealed class ClothingInInventoryCon : Consideration
    {
        public ClothingInInventoryCon Slot(EquipmentSlotDefines.SlotFlags slotFlags, Blackboard context)
        {
            // Ideally we'd just use a variable but then if we were iterating through multiple AI at once it'd be
            // Stuffed so we need to store it on the AI's context.
            context.GetState<ClothingSlotFlagConState>().SetValue(slotFlags);
            return this;
        }

        protected override float GetScore(Blackboard context)
        {
            var slots = context.GetState<ClothingSlotConState>().GetValue();
            var slotFlags = EquipmentSlotDefines.SlotMasks[slots];

            foreach (var entity in context.GetState<EnumerableInventoryState>().GetValue())
            {
                if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out ClothingComponent? clothingComponent))
                {
                    continue;
                }

                if ((clothingComponent.SlotFlags & slotFlags) != 0)
                {
                    return 1.0f;
                }
            }

            return 0.0f;
        }
    }
}
