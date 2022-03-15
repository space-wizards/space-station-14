using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Clothing;
using Content.Shared.Inventory;

namespace Content.Server.AI.Utility.Considerations.Clothing
{
    public class ClothingInSlotCon : Consideration
    {

        public ClothingInSlotCon Slot(string slot, Blackboard context)
        {
            context.GetState<ClothingSlotConState>().SetValue(slot);
            return this;
        }

        protected override float GetScore(Blackboard context)
        {
            var slot = context.GetState<ClothingSlotConState>().GetValue();
            var inventory = context.GetState<EquippedClothingState>().GetValue();
            return slot != null && inventory.ContainsKey(slot) ? 1.0f : 0.0f;
        }
    }
}
