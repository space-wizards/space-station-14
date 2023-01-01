using Content.Shared.Chemistry.Reagent;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.EntitySystems;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    public sealed class InCrit : ReagentEffectCondition
    {

        [Dependency] private readonly SharedMobStateSystem _mobState = default!;

        public override bool Condition(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.SolutionEntity, out MobStateComponent? mobState))
            {
                if (mobState.CurrentState == DamageState.Critical)
                    return true;
            }

            return false;
        }
    }
}

