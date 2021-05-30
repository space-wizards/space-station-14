using Content.Server.GameObjects.Components.Nutrition;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces.Chemistry;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.Metabolism
{
    /// <summary>
    /// Default metabolism for food reagents. Attempts to find a HungerComponent on the target,
    /// and to update it's hunger values.
    /// </summary>
    [DataDefinition]
    public class DefaultFood : IMetabolizable
    {
        /// <summary>
        ///     Rate of metabolism in units / second
        /// </summary>
        [DataField("rate")] public ReagentUnit MetabolismRate { get; private set; } = ReagentUnit.New(1.0);

        /// <summary>
        ///     How much hunger is satiated when 1u of the reagent is metabolized
        /// </summary>
        [DataField("nutritionFactor")] public float NutritionFactor { get; set; } = 30.0f;

        //Remove reagent at set rate, satiate hunger if a HungerComponent can be found
        ReagentUnit IMetabolizable.Metabolize(IEntity solutionEntity, string reagentId, float tickTime)
        {
            var metabolismAmount = MetabolismRate * tickTime;
            if (solutionEntity.TryGetComponent(out HungerComponent? hunger))
                hunger.UpdateFood(metabolismAmount.Float() * NutritionFactor);

            //Return amount of reagent to be removed, remove reagent regardless of HungerComponent presence
            return metabolismAmount;
        }
    }
}
