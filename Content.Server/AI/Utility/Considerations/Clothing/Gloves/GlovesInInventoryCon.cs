using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects;
using Content.Shared.GameObjects.Components.Inventory;

namespace Content.Server.AI.Utility.Considerations.Clothing.Gloves
{
    public sealed class GlovesInInventoryCon : Consideration
    {
        public GlovesInInventoryCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var inventory = context.GetState<InventoryState>().GetValue();

            foreach (var entity in inventory)
            {
                if (!entity.TryGetComponent(out ClothingComponent clothingComponent))
                {
                    continue;
                }

                if ((clothingComponent.SlotFlags & EquipmentSlotDefines.SlotFlags.GLOVES) != 0)
                {
                    return 1.0f;
                }
            }

            return 0.0f;
        }
    }
}
