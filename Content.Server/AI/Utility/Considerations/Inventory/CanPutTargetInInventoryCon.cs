using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Hands;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Shared.Item;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Considerations.Inventory
{
    public sealed class CanPutTargetInInventoryCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            // First check if target in inventory already
            // If not then check if we have a free hand
            var target = context.GetState<TargetEntityState>().GetValue();

            if (target == null || !IoCManager.Resolve<IEntityManager>().HasComponent<SharedItemComponent>(target))
            {
                return 0.0f;
            }

            foreach (var item in context.GetState<EnumerableInventoryState>().GetValue())
            {
                if (item == target)
                {
                    return 1.0f;
                }
            }

            return context.GetState<AnyFreeHandState>().GetValue() ? 1.0f : 0.0f;
        }
    }
}
