using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.EntitySystems;

namespace Content.Server.Chemistry.ReagentEffectConditions
{
    public sealed class RelativeDamage : ReagentEffectCondition
    {

        [Dependency] private readonly SharedMobStateSystem _mobState = default!;
        [DataField("max")]
        public float Max = 1F;

        [DataField("min")]
        public float Min = 0F;

        public override bool Condition(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.SolutionEntity, out DamageableComponent? damage) && args.EntityManager.TryGetComponent(args.SolutionEntity, out MobStateComponent? mobState))
            {
                var total = damage.TotalDamage;

                foreach (var (threshold, state) in mobState._lowestToHighestStates)
                {
                    if (DamageState.Dead == state)
                    {
                        var max = threshold * Max;
                        var min = threshold * Min;

                        if (total > min && total < max)
                        {
                            return true;
                        }
                        return false;
                    }
                }
            }

            return false;
        }
    }
}

