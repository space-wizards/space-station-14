using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Attempts to find a HungerComponent on the target,
    /// and to update it's hunger values.
    /// </summary>
    public sealed class SatiateHunger : ReagentEffect
    {
        /// <summary>
        ///     How much hunger is satiated when 1u of the reagent is metabolized
        /// </summary>
        [DataField("factor")] public float NutritionFactor { get; set; } = 3.0f;

        //Remove reagent at set rate, satiate hunger if a HungerComponent can be found
        public override void Effect(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.SolutionEntity, out HungerComponent? hunger))
                hunger.UpdateFood(NutritionFactor * (float) args.Quantity);
        }
    }
}
