using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Metabolizable;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.Metabolism
{
    /// <summary>
    /// Default metabolism for food reagents. Attempts to find a HungerComponent on the target,
    /// and to update it's hunger values. Inherits metabolisation rate logic from DefaultMetabolizable.
    /// </summary>
    [DataDefinition]
    public class DefaultFood : DefaultMetabolizable
    {

        /// <summary>
        ///     How much hunger is satiated when 1u of the reagent is metabolized
        /// </summary>
        [DataField("nutritionFactor")] public float NutritionFactor { get; set; } = 30.0f;


        //Remove reagent at set rate, satiate hunger if a HungerComponent can be found
        public override ReagentUnit Metabolize(IEntity solutionEntity, string reagentId, float tickTime, ReagentUnit availableReagent)
        {
            // use DefaultMetabolism to determine how much reagent we should metabolize
            var amountMetabolized = base.Metabolize(solutionEntity, reagentId, tickTime, availableReagent);

            // If metabolizing entity has a HungerComponent, feed them.
            if (solutionEntity.TryGetComponent(out HungerComponent? hunger))
                hunger.UpdateFood(amountMetabolized.Float() * NutritionFactor);

            //Return amount of reagent to be removed. Reagent is removed regardless of HungerComponent presence
            return amountMetabolized;
        }
    }
}
