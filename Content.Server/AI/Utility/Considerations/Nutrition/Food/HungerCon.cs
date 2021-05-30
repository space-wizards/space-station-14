using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.GameObjects.Components.Nutrition;

namespace Content.Server.AI.Utility.Considerations.Nutrition.Food
{

    public sealed class HungerCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            if (owner == null || !owner.TryGetComponent(out HungerComponent? hunger))
            {
                return 0.0f;
            }

            return 1 - (hunger.CurrentHunger / hunger.HungerThresholds[HungerThreshold.Overfed]);
        }
    }
}
