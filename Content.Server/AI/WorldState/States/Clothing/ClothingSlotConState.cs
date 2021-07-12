using Content.Shared.Inventory;

namespace Content.Server.AI.WorldState.States.Clothing
{
    public sealed class ClothingSlotConState : PlanningStateData<EquipmentSlotDefines.Slots>
    {
        public override string Name => "ClothingSlotCon";
        public override void Reset()
        {
            Value = EquipmentSlotDefines.Slots.NONE;
        }
    }
}
