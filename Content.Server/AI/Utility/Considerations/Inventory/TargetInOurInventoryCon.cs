using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components;

namespace Content.Server.AI.Utility.Considerations.Inventory
{
    public class TargetInOurInventoryCon : Consideration
    {
        public TargetInOurInventoryCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var inventory = context.GetState<InventoryState>().GetValue();
            var target = context.GetState<TargetEntityState>().GetValue();

            if (target == null || !target.HasComponent<ItemComponent>())
            {
                return 0.0f;
            }

            foreach (var item in inventory)
            {
                if (item == target)
                {
                    return 1.0f;
                }
            }

            return 0.0f;
        }
    }
}
