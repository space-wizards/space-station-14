using Content.Shared.Inventory;

namespace Content.Server.AI.WorldState.States.Clothing
{
    public sealed class ClothingSlotFlagConState : PlanningStateData<SlotFlags>
    {
        public override string Name => "ClothingSlotFlagCon";
        public override void Reset()
        {
            Value = SlotFlags.NONE;
        }
    }
}
