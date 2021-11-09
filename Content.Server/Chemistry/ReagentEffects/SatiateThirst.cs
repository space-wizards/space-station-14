using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Default metabolism for drink reagents. Attempts to find a ThirstComponent on the target,
    /// and to update it's thirst values.
    /// </summary>
    public class SatiateThirst : ReagentEffect
    {
        /// How much thirst is satiated each metabolism tick. Not currently tied to
        /// rate or anything.
        [DataField("factor")]
        public float HydrationFactor { get; set; } = 3.0f;

        /// Satiate thirst if a ThirstComponent can be found
        public override void Metabolize(EntityUid solutionEntity, EntityUid organEntity, Solution.ReagentQuantity reagent, IEntityManager entityManager)
        {
            if (entityManager.TryGetComponent(solutionEntity, out ThirstComponent? thirst))
                thirst.UpdateThirst(HydrationFactor);
        }
    }
}
