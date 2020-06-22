using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.GameObjects;

namespace Content.Server.AI.Utility.Considerations.Hands
{
    public class FreeHandCon : Consideration
    {
        public FreeHandCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            if (!owner.TryGetComponent(out HandsComponent handsComponent))
            {
                return 0.0f;
            }

            var handCount = 0;
            var freeCount = 0;

            foreach (var hand in handsComponent.ActivePriorityEnumerable())
            {
                handCount++;
                if (handsComponent.GetHand(hand) == null)
                {
                    freeCount += 1;
                }
            }

            return (float) freeCount / handCount;
        }
    }
}
