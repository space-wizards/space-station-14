using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.GameObjects.Components.Nutrition;

namespace Content.Server.AI.Utility.Considerations.Nutrition
{

    public sealed class HungerCon : Consideration
    {
        public HungerCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();

            if (!owner.TryGetComponent(out HungerComponent hunger))
            {
                return 0.0f;
            }

            return 1 - (hunger.CurrentHunger / hunger.HungerThresholds[HungerThreshold.Overfed]);
        }
    }
}
