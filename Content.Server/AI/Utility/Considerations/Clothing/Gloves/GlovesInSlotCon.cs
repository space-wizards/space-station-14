using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Clothing;
using Content.Shared.GameObjects.Components.Inventory;

namespace Content.Server.AI.Utility.Considerations.Clothing.Gloves
{
    public class GlovesInSlotCon : Consideration
    {
        public GlovesInSlotCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var inventory = context.GetState<EquippedClothingState>().GetValue();

            return inventory.ContainsKey(EquipmentSlotDefines.Slots.GLOVES) ? 1.0f : 0.0f;
        }
    }
}
