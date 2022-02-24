using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.Hands.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Considerations.Hands
{
    public sealed class FreeHandCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            if (!owner.IsValid() || !IoCManager.Resolve<IEntityManager>().TryGetComponent(owner, out HandsComponent? handsComponent))
            {
                return 0.0f;
            }

            var handCount = 0;
            var freeCount = 0;

            foreach (var hand in handsComponent.ActivePriorityEnumerable())
            {
                handCount++;
                if (handsComponent.GetItem(hand) == null)
                {
                    freeCount += 1;
                }
            }

            return (float) freeCount / handCount;
        }
    }
}
