using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    public sealed partial class TotalDamage : ReagentEffectCondition
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

        public override string GuidebookExplanation(IPrototypeManager prototype)
        {
            return Loc.GetString("reagent-effect-condition-guidebook-total-damage",
                ("max", Max == FixedPoint2.MaxValue ? (float) int.MaxValue : Max.Float()),
                ("min", Min.Float()));
        }
    }
}
