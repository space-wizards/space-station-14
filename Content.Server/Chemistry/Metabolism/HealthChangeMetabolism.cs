using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;


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
        /// How much of the reagent should be metabolized each sec
        /// </summary> 
        [DataField("rate")]
        public ReagentUnit MetabolismRate { get; set; } = ReagentUnit.New(1);

        /// <summary>
        /// How much damage is changed when 1u of the reagent is metabolized
        /// </summary>
        [DataField("healthChange")]
        public float HealthChange { get; set; } = 1.0f;

        /// <summary>
        /// type of damage changed
        /// </summary> 
        [DataField("damageClass")]
        public DamageClass DamageType { get; set; } =  DamageClass.Brute;
        

        /// <summary>
        /// Remove reagent at set rate, changes damage if a DamageableComponent can be found
        /// </summary>
        /// <param name="solutionEntity"></param>
        /// <param name="reagentId"></param>
        /// <param name="tickTime"></param>
        /// <returns></returns>
        ReagentUnit IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime)
        {
            var metabolismAmount = ReagentUnit.New(MetabolismRate.Float() * tickTime * 100);
            if (solutionEntity.TryGetComponent(out IDamageableComponent? health))
                health.ChangeDamage(DamageType, (int) (metabolismAmount.Float() * HealthChange), true);

            return metabolismAmount;
        }
    }
}
