using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.GameObjects.Components.GUI;

namespace Content.Server.AI.Utility.Considerations.Hands
{
    public class FreeHandCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            if (owner == null || !owner.TryGetComponent(out HandsComponent? handsComponent))
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
