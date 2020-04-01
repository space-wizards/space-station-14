using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Clothing;
using Content.Shared.GameObjects.Components.Inventory;

namespace Content.Server.AI.Utility.Considerations.Clothing.OuterClothing
{
    public class OuterClothingInSlotCon : Consideration
    {
        public OuterClothingInSlotCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var inventory = context.GetState<EquippedClothingState>().GetValue();

            return inventory.ContainsKey(EquipmentSlotDefines.Slots.OUTERCLOTHING) ? 1.0f : 0.0f;
        }
    }
}
