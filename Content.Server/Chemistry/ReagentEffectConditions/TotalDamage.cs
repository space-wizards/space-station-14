using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    public sealed class TotalDamage : ReagentEffectCondition
    {
        [DataField("max")]
        public FixedPoint2 Max = FixedPoint2.MaxValue;

        [DataField("min")]
        public FixedPoint2 Min = FixedPoint2.Zero;

        public override bool Condition(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.SolutionEntity, out DamageableComponent? damage))
            {
                var total = damage.TotalDamage;
                if (total > Min && total < Max)
                    return true;
            }

            return false;
        }
    }
}
