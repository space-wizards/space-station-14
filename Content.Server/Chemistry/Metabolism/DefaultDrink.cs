using System;
using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using SolutionComponent = Content.Shared.GameObjects.Components.Chemistry.SolutionComponent;

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

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(ref _metabolismRate, "rate", 1);
            serializer.DataField(ref _hydrationFactor, "nutrimentFactor", 30.0f);
        }

        public int Metabolize(IEntity solutionEntity, string reagentId, float frameTime)
        {
            if (!solutionEntity.TryGetComponent(out ThirstComponent thirst))
                return 0;

            int metabolismAmount = (int)Math.Round(MetabolismRate * frameTime);
            thirst.UpdateThirst(metabolismAmount * HydrationFactor);
            return metabolismAmount; //Return amount of reagent to be removed
        }
    }
}
