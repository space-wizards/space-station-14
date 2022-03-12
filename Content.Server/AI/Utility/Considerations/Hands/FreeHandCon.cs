using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.Hands.Components;
using System.Linq;

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

            return (float) handsComponent.Hands.Values.Count(hand => hand.IsEmpty) / handsComponent.Hands.Count;
        }
    }
}
