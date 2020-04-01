using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Clothing;
using Content.Shared.GameObjects.Components.Inventory;

namespace Content.Server.AI.Utility.Considerations.Clothing.Head
{
    public class HeadInSlotCon : Consideration
    {
        public HeadInSlotCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var inventory = context.GetState<EquippedClothingState>().GetValue();

            return inventory.ContainsKey(EquipmentSlotDefines.Slots.HEAD) ? 1.0f : 0.0f;
        }
    }
}
