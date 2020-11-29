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
    /// and to update it's damage values.
    /// </summary>
    class DefaultMedicine : IMetabolizable
    {
        //Rate of metabolism in units / second
        private ReagentUnit _metabolismRate;
        public ReagentUnit MetabolismRate => _metabolismRate;


        //How much damage is healed when 1u of the reagent is metabolized
        private float _healFactor;
        private DamageClass _healType;
        private DamageClass healType => _healType;
        public float healFactor => _healFactor;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _metabolismRate, "rate", ReagentUnit.New(1));
            serializer.DataField(ref _healFactor, "healFactor", -1f);
            serializer.DataField(ref _healType, "healType", DamageClass.Brute);
        }

        //Remove reagent at set rate, heal damage if a DamageableComponent can be found
        ReagentUnit IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime)
        {
            var metabolismAmount = MetabolismRate * tickTime;

            if (solutionEntity.TryGetComponent(out DamageableComponent health))

                health.ChangeDamage(healType, -(int) (metabolismAmount.Float() * healFactor), true);

            return metabolismAmount;
        }
    }
}
