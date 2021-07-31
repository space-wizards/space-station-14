using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Metabolizable;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.Metabolism
{
    /// <summary>
    /// Default metabolism for drink reagents. Attempts to find a ThirstComponent on the target,
    /// and to update it's thirst values. Inherits metabolisation rate logic from DefaultMetabolizable.
    /// </summary>
    [DataDefinition]
    public class DefaultDrink : DefaultMetabolizable
    {
        //How much thirst is satiated when 1u of the reagent is metabolized
        [DataField("hydrationFactor")]
        public float HydrationFactor { get; set; } = 30.0f;

        //Remove reagent at set rate, satiate thirst if a ThirstComponent can be found
        public override ReagentUnit Metabolize(IEntity solutionEntity, string reagentId, float tickTime, ReagentUnit availableReagent)
        {
            // use DefaultMetabolism to determine how much reagent we should metabolize
            var amountMetabolized = base.Metabolize(solutionEntity, reagentId, tickTime, availableReagent);

            // If metabolizing entity has a ThirstComponent, hydrate them.
            if (solutionEntity.TryGetComponent(out ThirstComponent? thirst))
                thirst.UpdateThirst(amountMetabolized.Float() * HydrationFactor);

            //Return amount of reagent to be removed, remove reagent regardless of ThirstComponent presence
            return amountMetabolized;
        }
    }
}
