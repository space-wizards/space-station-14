using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    public sealed class MobStateCondition : ReagentEffectCondition
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
    }
}

