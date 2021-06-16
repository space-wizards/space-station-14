using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Content.Shared.Damage;
using Content.Shared.Chemistry.Metabolizable;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Components;
using Content.Server.Chemistry.Components;

namespace Content.Server.Chemistry.Metabolism
{
    /// <summary>
    /// Deals a certain amount of damage once a certain amount of reagent is ingested at once.
    /// </summary>
    [DataDefinition]
    public class OverdoseMetabolism : IMetabolizable
    {
        /// <summary>
        /// How much of the reagent should be metabolized each sec.
        /// </summary> 
        [DataField("rate")]
        public ReagentUnit MetabolismRate { get; set; } = ReagentUnit.New(1);

        /// <summary>
        /// How much damage is changed when the overdose happenes, allowing for possible positive overdose?
        /// </summary>
        [DataField("healthChange")]
        public float HealthChange { get; set; } = 500.0f;

        /// <summary>
        /// Class of damage changed, Brute, Burn, Toxin, Airloss.
        /// </summary> 
        [DataField("damageClass")]
        public DamageClass DamageType { get; set; } = DamageClass.Brute;

        [DataField("overdoseReagent")]
        public string OverdoseReagent { get; set; } = "Water";

        [DataField("overdoseAmount")]
        public ReagentUnit OverdoseAmount { get; set; } = ReagentUnit.New(30.0f);

        /// <summary>
        /// Remove reagent at set rate, changes damage if a an overdose happens.
        /// </summary>
        /// <param name="solutionEntity"></param>
        /// <param name="reagentId"></param>
        /// <param name="tickTime"></param>
        /// <returns></returns>
        ReagentUnit IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime)
        {
            if (solutionEntity.TryGetComponent(out IDamageableComponent? health) && solutionEntity.TryGetComponent(out SolutionContainerComponent? solutionComponent))
            {

                solutionComponent.Solution.ContainsReagent(OverdoseReagent, out ReagentUnit chemicalAmount);
                    
                if (chemicalAmount >= OverdoseAmount)
                {
                    health.ChangeDamage(DamageType, (int) HealthChange, true);
                }
            }
            return MetabolismRate;
        }
    }
}
