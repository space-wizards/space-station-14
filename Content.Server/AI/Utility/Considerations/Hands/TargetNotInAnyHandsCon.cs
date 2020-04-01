using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.GameObjects;

namespace Content.Server.AI.Utility.Considerations.Hands
{
    /// <summary>
    /// Returns 0 if target equipped by anything, else 1
    /// I know you should avoid doing "not X" but in this case it also rules out no target // unequippable
    /// </summary>
    public sealed class TargetNotInAnyHandsCon : Consideration
    {
        public TargetNotInAnyHandsCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var target = context.GetState<TargetEntityState>().GetValue();
            if (target == null || !target.TryGetComponent(out ItemComponent itemComponent) || itemComponent.IsHeld)
            {
                return 0.0f;
            }

            return 1.0f;
        }
    }
}
