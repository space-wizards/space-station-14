using System;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.Metabolism
{
    /// <summary>
    /// Default metabolism for drink reagents. Attempts to find a ThirstComponent on the target,
    /// and to update it's thirst values.
    /// </summary>
    class DefaultDrink : IMetabolizable
    {
        //Rate of metabolism in units / second
        private int _metabolismRate;
        public int MetabolismRate => _metabolismRate;

        //How much thirst is satiated when 1u of the reagent is metabolized
        private float _hydrationFactor;
        public float HydrationFactor => _hydrationFactor;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _metabolismRate, "rate", 1);
            serializer.DataField(ref _hydrationFactor, "nutrimentFactor", 30.0f);
        }

        //Remove reagent at set rate, satiate thirst if a ThirstComponent can be found
        decimal IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime)
        {
            int metabolismAmount = (int)Math.Round(MetabolismRate * tickTime);
            if (solutionEntity.TryGetComponent(out ThirstComponent thirst))
                thirst.UpdateThirst(metabolismAmount * HydrationFactor);

            //Return amount of reagent to be removed, remove reagent regardless of ThirstComponent presence
            return metabolismAmount;
        }
    }
}
