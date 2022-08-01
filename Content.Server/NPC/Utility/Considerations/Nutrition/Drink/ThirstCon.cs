using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.Nutrition.Components;

namespace Content.Server.NPC.Utility.Considerations.Nutrition.Drink
{
    public sealed class ThirstCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(owner, out ThirstComponent? thirst))
            {
                return 0.0f;
            }

            return 1 - (thirst.CurrentThirst / thirst.ThirstThresholds[ThirstThreshold.OverHydrated]);
        }
    }
}
