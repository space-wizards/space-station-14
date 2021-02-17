using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.Metabolism
{
    /// <summary>
    /// Default metabolism for drink reagents. Attempts to find a ThirstComponent on the target,
    /// and to update it's thirst values.
    /// </summary>
    public class DefaultDrink : IMetabolizable
    {
        //Rate of metabolism in units / second
        private ReagentUnit _metabolismRate;
        public ReagentUnit MetabolismRate => _metabolismRate;

        //How much thirst is satiated when 1u of the reagent is metabolized
        private float _hydrationFactor;
        public float HydrationFactor => _hydrationFactor;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _metabolismRate, "rate", ReagentUnit.New(1));
            serializer.DataField(ref _hydrationFactor, "nutrimentFactor", 30.0f);
        }

        //Remove reagent at set rate, satiate thirst if a ThirstComponent can be found
        ReagentUnit IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime)
        {
            var metabolismAmount = MetabolismRate * tickTime;
            if (solutionEntity.TryGetComponent(out ThirstComponent thirst))
                thirst.UpdateThirst(metabolismAmount.Float() * HydrationFactor);

            //Return amount of reagent to be removed, remove reagent regardless of ThirstComponent presence
            return metabolismAmount;
        }
    }
}
