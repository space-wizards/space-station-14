using Content.Server.AI.Utility.Curves;
using Content.Server.AI.Utils;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;

namespace Content.Server.AI.Utility.Considerations.Combat.Ranged
{
    public class HasTargetLosCon : Consideration
    {
        public HasTargetLosCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();
            var target = context.GetState<TargetEntityState>().GetValue();
            if (target == null)
            {
                return 0.0f;
            }

            return Visibility.InLineOfSight(owner, target) ? 1.0f : 0.0f;
        }
    }
}
