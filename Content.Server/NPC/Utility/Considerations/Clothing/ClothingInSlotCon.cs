using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States.Clothing;

namespace Content.Server.NPC.Utility.Considerations.Clothing
{
    public sealed class ClothingInSlotCon : Consideration
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
