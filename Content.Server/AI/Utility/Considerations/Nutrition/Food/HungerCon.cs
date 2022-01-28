using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.Nutrition.Components;
using Content.Shared.Nutrition.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Considerations.Nutrition.Food
{

    public sealed class HungerCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(owner, out HungerComponent? hunger))
            {
                return 0.0f;
            }

            return 1 - (hunger.CurrentHunger / hunger.HungerThresholds[HungerThreshold.Overfed]);
        }
    }
}
