using System;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.Interfaces.Chemistry;
using Content.Shared.Maths;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.Metabolism
{
    /// <summary>
    /// Default metabolism for food reagents. Attempts to find a HungerComponent on the target,
    /// and to update it's hunger values.
    /// </summary>
    class DefaultFood : IMetabolizable
    {
        //Rate of metabolism in units / second
        private decimal _metabolismRate;
        public decimal MetabolismRate => _metabolismRate;

        //How much hunger is satiated when 1u of the reagent is metabolized
        private decimal _nutritionFactor;
        public decimal NutritionFactor => _nutritionFactor;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _metabolismRate, "rate", 1M);
            serializer.DataField(ref _nutritionFactor, "nutrimentFactor", 30.0M);
        }

        //Remove reagent at set rate, satiate hunger if a HungerComponent can be found
        decimal IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime)
        {
            var metabolismAmount = (MetabolismRate * (decimal) tickTime).RoundForReagents();
            if (solutionEntity.TryGetComponent(out HungerComponent hunger))
                hunger.UpdateFood((float)(metabolismAmount * NutritionFactor));

            //Return amount of reagent to be removed, remove reagent regardless of HungerComponent presence
            return metabolismAmount; 
        }
    }
}
