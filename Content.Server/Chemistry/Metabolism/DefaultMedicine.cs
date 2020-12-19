using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.Metabolism
{
    /// <summary>
    /// Default metabolism for medicine reagents. Attempts to find a DamegableComponent on the target,
    /// and to update its damage values in accordance with the heal amount.
    /// </summary>
    public class DefaultMedicine : IMetabolizable
    {
        
        //<summary>
        //Rate of metabolism in units / seco
        //</summary>
        private ReagentUnit _metabolismRate;
        public ReagentUnit MetabolismRate => _metabolismRate;

        //<summary>
        //How much damage is healed when 1u of the reagent is metabolized
        //</summary>
        private float _healingPerSec;
        private DamageClass _healType;
        private DamageClass healType => _healType;
        public float HealingPerSec => _healingPerSec;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _metabolismRate, "rate", ReagentUnit.New(1));
            serializer.DataField(ref _healingPerSec, "healingPerSec", 1f);
            serializer.DataField(ref _healType, "healType", DamageClass.Brute);
        }
        
        //<summary>
        //Remove reagent at set rate, heal damage if a DamageableComponent can be found
        //</summary>
        ReagentUnit IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime)
        {
            var metabolismAmount = MetabolismRate * tickTime;
            if (solutionEntity.TryGetComponent(out DamageableComponent health))
                health.ChangeDamage(healType, -(int)(metabolismAmount.Float() * HealingPerSec), true);

            return metabolismAmount;
        }
    }
}
