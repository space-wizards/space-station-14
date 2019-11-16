using System;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using SolutionComponent = Content.Shared.GameObjects.Components.Chemistry.SolutionComponent;

namespace Content.Server.Chemistry.Metabolism
{
    /// <summary>
    /// Default metabolism for food reagents. Attempts to find a HungerComponent on the target,
    /// and to update it's hunger values.
    /// </summary>
    class DefaultFood : IMetabolizable
    {
        //Rate of metabolism in units / second
        private int _metabolismRate;
        public int MetabolismRate => _metabolismRate;

        //How much hunger is satiated when 1u of the reagent is metabolized
        private float _nutritionFactor;
        public float NutritionFactor => _nutritionFactor;

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _metabolismRate, "rate", 1);
            serializer.DataField(ref _nutritionFactor, "nutrimentFactor", 30.0f);
        }

        public int Metabolize(IEntity solutionEntity, string reagentId, float frameTime)
        {
            if (!solutionEntity.TryGetComponent(out HungerComponent hunger))
                return 0;

            int metabolismAmount = (int)Math.Round(MetabolismRate * frameTime);
            hunger.UpdateFood(metabolismAmount * NutritionFactor);
            return metabolismAmount; //Return amount of reagent to be removed
        }
    }
}
