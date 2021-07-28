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
    /// and to update its damage values.
    /// </summary>
    [DataDefinition]
    public class HealthChangeMetabolism : IMetabolizable
    {
        /// <summary>
        /// How much of the reagent should be metabolized each sec.
        /// </summary>
        [DataField("rate")]
        public ReagentUnit MetabolismRate { get; set; } = ReagentUnit.New(1);

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
        /// <param name="availableReagant">Reagant available to be metabolized.</param>
        /// <returns></returns>
        ReagentUnit IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime, ReagentUnit availableReagent)
        {
            // how much reagant should we metabolize
            var metabolismAmount = MetabolismRate * tickTime;

            // is that much reagant actually available?
            if (availableReagent < metabolismAmount) {
                metabolismAmount = availableReagent;
            }

            // how much does this much reagant heal for
            var healthChangeAmmount = HealthChange * metabolismAmount.Float();

            if (solutionEntity.TryGetComponent(out IDamageableComponent? health))
            {
                // Heal damage by healthChangeAmmount, rounding down to nearest integer
                health.ChangeDamage(DamageType, (int) healthChangeAmmount, true);

                // Store decimal remainder of healthChangeAmmount in _accumulatedHealth
                _accumulatedHealth += (healthChangeAmmount - (int) healthChangeAmmount);

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
            return metabolismAmount;
        }
    }
}
