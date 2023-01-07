using Content.Shared.Chemistry.Reagent;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.EntitySystems;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    public sealed class DamageStateCondition : ReagentEffectCondition
    {

        [Dependency] private readonly SharedMobStateSystem _mobState = default!;

        [DataField("damagestate")]
        public DamageState DamageState = DamageState.Alive;

        public override bool Condition(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.SolutionEntity, out MobStateComponent? mobState))
            {
                if (mobState.CurrentState == DamageState)
                    return true;
            }

            return false;
        }
    }
}

