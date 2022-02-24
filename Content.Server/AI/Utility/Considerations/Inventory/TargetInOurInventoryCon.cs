using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Shared.Item;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Considerations.Inventory
{
    public sealed class TargetInOurInventoryCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
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

            return 0.0f;
        }
    }
}
