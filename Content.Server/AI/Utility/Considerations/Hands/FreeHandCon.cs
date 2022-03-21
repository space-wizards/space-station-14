using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Shared.Hands.Components;
using System.Linq;

namespace Content.Server.AI.Utility.Considerations.Hands
{
    public sealed class FreeHandCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            if (!owner.IsValid() || !IoCManager.Resolve<IEntityManager>().TryGetComponent(owner, out SharedHandsComponent? handsComponent))
            {
                return 0.0f;
            }

            return (float) handsComponent.CountFreeHands() / handsComponent.Count;
        }
    }
}
