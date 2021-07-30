using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Attempts to find a HungerComponent on the target,
    /// and to update it's hunger values.
    /// </summary>
    public class SatiateHunger : ReagentEffect
    {
        /// <summary>
        ///     How much hunger is satiated when 1u of the reagent is metabolized
        /// </summary>
        [DataField("nutritionFactor")] public float NutritionFactor { get; set; } = 3.0f;

        //Remove reagent at set rate, satiate hunger if a HungerComponent can be found
        public override void Metabolize(IEntity solutionEntity, Solution.ReagentQuantity amount)
        {
            if (solutionEntity.TryGetComponent(out HungerComponent? hunger))
                hunger.UpdateFood(NutritionFactor);
        }
    }
}
