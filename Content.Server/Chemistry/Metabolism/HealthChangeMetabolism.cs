using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Metabolizable;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;

namespace Content.Server.Chemistry.Metabolism
{
    /// <summary>
    /// Default metabolism for medicine reagents. Attempts to find a DamageableComponent on the target,
    /// and to update its damage values. Inherits metabolisation rate logic from DefaultMetabolizable.
    /// </summary>
    [DataDefinition]
    public class HealthChangeMetabolism : DefaultMetabolizable
    {

        /// <summary>
        /// How much damage is changed when 1u of the reagent is metabolized.
        /// </summary>
        [DataField("healthChange")]
        public float HealthChange { get; set; } = 1.0f;

        /// <summary>
        /// Class of damage changed, Brute, Burn, Toxin, Airloss.
        /// </summary>
        [DataField("damageClass")]
        public DamageClass DamageType { get; set; } =  DamageClass.Brute;

        private float _accumulatedHealth;

        /// <summary>
        /// Remove reagent at set rate, changes damage if a DamageableComponent can be found.
        /// </summary>
        /// <param name="solutionEntity"></param>
        /// <param name="reagentId"></param>
        /// <param name="tickTime"></param>
        /// <param name="availableReagent">Reagent available to be metabolized.</param>
        /// <returns></returns>
        public override ReagentUnit Metabolize(IEntity solutionEntity, string reagentId, float tickTime, ReagentUnit availableReagent)
        {
            // use DefaultMetabolism to determine how much reagent we should metabolize
            var amountMetabolized = base.Metabolize(solutionEntity, reagentId, tickTime, availableReagent);

            // how much does this much reagent heal for
            var healthChangeAmount = HealthChange * amountMetabolized.Float();

            if (solutionEntity.TryGetComponent(out IDamageableComponent? health))
            {
                // Heal damage by healthChangeAmmount, rounding down to nearest integer
                health.ChangeDamage(DamageType, (int) healthChangeAmount, true);

                // Store decimal remainder of healthChangeAmmount in _accumulatedHealth
                _accumulatedHealth += (healthChangeAmount - (int) healthChangeAmount);

                if (_accumulatedHealth >= 1)
                {
                    health.ChangeDamage(DamageType, 1, true);
                    _accumulatedHealth -= 1;
                }

                else if(_accumulatedHealth <= -1)
                {
                    health.ChangeDamage(DamageType, -1, true);
                    _accumulatedHealth += 1;
                }
            }
            return amountMetabolized;
        }
    }
}
