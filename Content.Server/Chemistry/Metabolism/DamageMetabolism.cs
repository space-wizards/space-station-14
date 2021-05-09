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
    public class DamageMetabolism : IMetabolizable
    {
        //Rate of metabolism in units / second
        [DataField("rate")]
        public ReagentUnit MetabolismRate { get; set; } = ReagentUnit.New(1);

        //How much damage is done/healed when 1u of the reagent is metabolized
        [DataField("healthChange")]
        public float HealthChange { get; set; } = 30.0f;

        //type of damage changed 
        [DataField("damageType")]
        public DamageClass DamageType { get; set; } =  DamageClass.Brute;
        


        //Remove reagent at set rate, changes damage if a DamageableComponent can be found
        ReagentUnit IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime)
        {
            var metabolismAmount = ReagentUnit.New(MetabolismRate.Float() * tickTime * 100);
            if (solutionEntity.TryGetComponent(out IDamageableComponent? health))
                health.ChangeDamage(DamageType, (int) (metabolismAmount.Float() * HealthChange), true);

            return metabolismAmount;
        }
    }
}
