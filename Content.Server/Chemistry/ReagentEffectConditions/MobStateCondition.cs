using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    public sealed partial class MobStateCondition : ReagentEffectCondition
    {


        [DataField("mobstate")]
        public MobState mobstate = MobState.Alive;

        public override bool Condition(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.SolutionEntity, out MobStateComponent? mobState))
            {
                if (mobState.CurrentState == mobstate)
                    return true;
            }

            return false;
        }

        public override string GuidebookExplanation(IPrototypeManager prototype)
        {
            return Loc.GetString("reagent-effect-condition-guidebook-mob-state-condition", ("state", mobstate));
        }
    }
}

