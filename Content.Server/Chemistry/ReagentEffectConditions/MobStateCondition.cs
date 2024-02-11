using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    public sealed partial class MobStateCondition : ReagentEffectCondition
    {
        [DataField]
        public MobState Mobstate = MobState.Alive;

        public override bool Condition(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.SolutionEntity, out MobStateComponent? mobState))
            {
                if (mobState.CurrentState == Mobstate)
                    return true;
            }

            return false;
        }

        public override string GuidebookExplanation(IPrototypeManager prototype)
        {
            return Loc.GetString("reagent-effect-condition-guidebook-mob-state-condition", ("state", Mobstate));
        }
    }
}

