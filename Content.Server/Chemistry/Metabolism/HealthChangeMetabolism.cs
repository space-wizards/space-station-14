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
        /// <returns></returns>
        ReagentUnit IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime)
        {
            if (solutionEntity.TryGetComponent(out IDamageableComponent? health))
            {
                health.ChangeDamage(DamageType, (int)HealthChange, true);
                float decHealthChange = (float) (HealthChange - (int) HealthChange);
                _accumulatedHealth += decHealthChange;

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
            return MetabolismRate;
        }
    }
}
