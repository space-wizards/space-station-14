using Content.Shared.Chemistry.Reagent;
using Content.Shared.Nutrition.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    public sealed partial class Hunger : ReagentEffectCondition
    {
        [DataField]
        public float Max = float.PositiveInfinity;

        [DataField]
        public float Min = 0;

        public override bool Condition(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.SolutionEntity, out HungerComponent? hunger))
            {
                var total = hunger.CurrentHunger;
                if (total > Min && total < Max)
                    return true;
            }

            return false;
        }

        public override string GuidebookExplanation(IPrototypeManager prototype)
        {
            return Loc.GetString("reagent-effect-condition-guidebook-total-hunger",
                ("max", float.IsPositiveInfinity(Max) ? (float) int.MaxValue : Max),
                ("min", Min));
        }
    }
}
