using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.GameObjects;

namespace Content.Server.AI.Utility.Considerations.Hands
{
    /// <summary>
    /// Returns 1 if in our hands else 0
    /// </summary>
    public sealed class TargetInOurHandsCon : Consideration
    {
        public TargetInOurHandsCon(IResponseCurve curve) : base(curve)
        {
        }

        public override float GetScore(Blackboard context)
        {
            var owner = context.GetState<SelfState>().GetValue();
            var target = context.GetState<TargetEntityState>().GetValue();

            if (target == null || !target.TryGetComponent(out ItemComponent itemComponent) || itemComponent.Holder != owner)
            {
                return 0.0f;
            }

            // This prroobbbbabbbllyy shouldn't happen but just in case?
            if (!owner.TryGetComponent(out HandsComponent _))
            {
                return 0.0f;
            }

            return 1.0f;
        }
    }
}
