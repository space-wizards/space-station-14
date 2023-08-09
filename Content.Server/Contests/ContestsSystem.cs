using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Physics.Components;

namespace Content.Server.Contests
{
    /// <summary>
    /// Standardized contests.
    /// A contest is figuring out, based on data in components on two entities,
    /// which one has an advantage in a situation. The advantage is expressed by a multiplier.
    /// 1 = No advantage to either party.
    /// &gt;1 = Advantage to roller
    /// &lt;1 = Advantage to target
    /// Roller should be the entity with an advantage from being bigger/healthier/more skilled, etc.
    /// </summary>
    ///
    public sealed class ContestsSystem : EntitySystem
    {
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
        /// <summary>
        /// Returns the roller's mass divided by the target's.
        /// </summary>
        public float MassContest(EntityUid roller, EntityUid target, PhysicsComponent? rollerPhysics = null, PhysicsComponent? targetPhysics = null)
        {
            if (!Resolve(roller, ref rollerPhysics, false) || !Resolve(target, ref targetPhysics, false))
                return 1f;

            if (targetPhysics.FixturesMass == 0)
                return 1f;

            return rollerPhysics.FixturesMass / targetPhysics.FixturesMass;
        }

        /// <summary>
        /// Tries to compare both entities damage to the damage they will enter crit at.
        /// After that, it runs the % towards crit through a converter that smooths out and makes
        /// the the ratios less severe.
        /// Returns the roller's adjusted damage value divided by the target's. Higher is better for the roller.
        /// </summary>
        public float DamageContest(EntityUid roller, EntityUid target, DamageableComponent? rollerDamage = null, DamageableComponent? targetDamage = null)
        {
            if (!Resolve(roller, ref rollerDamage, false) || !Resolve(target, ref targetDamage, false))
                return 1f;

            // First, we'll see what health they go into crit at.
            //TODO: refactor this entire system.... Why does this exist, this shouldn't be calculated off health
            var rollerThreshold = 100f;
            if (!_mobThresholdSystem.TryGetThresholdForState(roller, MobState.Critical, out var rollerCritThreshold))
            {
                if (_mobThresholdSystem.TryGetThresholdForState(roller, MobState.Critical,
                        out var rollerdeadThreshold))
                {
                    rollerThreshold = rollerdeadThreshold.Value.Float();
                }
            }
            else
            {
                rollerThreshold = rollerCritThreshold.Value.Float();
            }
            var targetThreshold = 100f;
            if (!_mobThresholdSystem.TryGetThresholdForState(roller, MobState.Critical, out var targetCritThreshold))
            {
                if (_mobThresholdSystem.TryGetThresholdForState(roller, MobState.Critical,
                        out var targetdeadThreshold))
                {
                    targetThreshold = targetdeadThreshold.Value.Float();
                }
            }
            else
            {
                targetThreshold = targetCritThreshold.Value.Float();
            }

            // Next, we'll see how their damage compares
            var rollerDamageScore = (float) rollerDamage.TotalDamage / rollerThreshold;
            var targetDamageScore = (float) targetDamage.TotalDamage / targetThreshold;

            return DamageThresholdConverter(rollerDamageScore) / DamageThresholdConverter(targetDamageScore);
        }

        /// <summary>
        /// Finds the % of each entity's stamina damage towards its stam crit threshold.
        /// It then runs the % towards the converter to smooth/soften it.
        /// Returns the roller's decimal % divided by the target's. Higher value is better for the roller.
        /// </summary>
        public float StaminaContest(EntityUid roller, EntityUid target, StaminaComponent? rollerStamina = null, StaminaComponent? targetStamina = null)
        {
            if (!Resolve(roller, ref rollerStamina, false) || !Resolve(target, ref targetStamina, false))
                return 1f;

            var rollerDamageScore= rollerStamina.StaminaDamage / rollerStamina.CritThreshold;
            var targetDamageScore = targetStamina.StaminaDamage / targetStamina.CritThreshold;

            return DamageThresholdConverter(rollerDamageScore) / DamageThresholdConverter(targetDamageScore);
        }

        /// <summary>
        /// This will compare the roller and target's damage, mass, and stamina. See the functions for what each one does.
        /// You can change the 'weighting' to make the tests weigh more or less than the other tests.
        /// Returns a weighted average of all 3 tests.
        /// </summary>
        public float OverallStrengthContest(EntityUid roller, EntityUid target, float damageWeight = 1f, float massWeight = 1f, float stamWeight = 1f)
        {
            var weightTotal = damageWeight + massWeight + stamWeight;
            var damageMultiplier = damageWeight / weightTotal;
            var massMultiplier = massWeight / weightTotal;
            var stamMultiplier = stamWeight / weightTotal;

            return DamageContest(roller, target) * damageMultiplier +
                   MassContest(roller, target) * massMultiplier +
                   StaminaContest(roller, target) * stamMultiplier;
        }

        /// <summary>
        /// This softens out the huge advantages that damage contests would lead to otherwise.
        /// Once you are crit or near crit, we just let the massive advantages roll with what could be a 20x.
        /// </summary>
        public float DamageThresholdConverter(float score)
        {
            return score switch
            {
                // TODO: Should just be a curve
                <= 0 => 1f,
                <= 0.25f => 0.9f,
                <= 0.5f => 0.75f,
                <= 0.75f => 0.6f,
                <= 0.95f => 0.45f,
                _ => 0.05f
            };
        }
    }
}
