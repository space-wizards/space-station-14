using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.Metabolism
{
    /// <summary>
    /// Default metabolism for food reagents. Attempts to find a HungerComponent on the target,
    /// and to update it's hunger values.
    /// </summary>
    public class DefaultFood : IMetabolizable
    {
        //Rate of metabolism in units / second
        private ReagentUnit _metabolismRate;
        public ReagentUnit MetabolismRate => _metabolismRate;

        //How much hunger is satiated when 1u of the reagent is metabolized
        private float _nutritionFactor;
        public float NutritionFactor => _nutritionFactor;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _metabolismRate, "rate", ReagentUnit.New(1.0));
            serializer.DataField(ref _nutritionFactor, "nutrimentFactor", 30.0f);
        }

        //Remove reagent at set rate, satiate hunger if a HungerComponent can be found
        ReagentUnit IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime)
        {
            var metabolismAmount = MetabolismRate * tickTime;
            if (solutionEntity.TryGetComponent(out HungerComponent hunger))
                hunger.UpdateFood(metabolismAmount.Float() * NutritionFactor);

            //Return amount of reagent to be removed, remove reagent regardless of HungerComponent presence
            return metabolismAmount;
        }
    }
}
