namespace Content.Server.NPC.WorldState.States.Clothing
{
    public sealed class ClothingSlotConState : PlanningStateData<string>
    {
        public override string Name => "ClothingSlotCon";
        public override void Reset()
        {
            Value = "";
        }
    }
}
