using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Clothing;
using Content.Shared.GameObjects.Components.Inventory;

namespace Content.Server.AI.Utility.Considerations.Clothing
{
    public class ClothingInSlotCon : Consideration
    {
        private EquipmentSlotDefines.Slots _slot;

        public ClothingInSlotCon(EquipmentSlotDefines.Slots slot, IResponseCurve curve) : base(curve)
        {
            _slot = slot;
        }

        public override float GetScore(Blackboard context)
        {
            var inventory = context.GetState<EquippedClothingState>().GetValue();

            return inventory.ContainsKey(_slot) ? 1.0f : 0.0f;
        }
    }
}
