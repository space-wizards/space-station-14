using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

/// <summary>
///     Handles removal of seeds on a plant.
/// </summary>

public sealed partial class PlantDestroySeeds : EventEntityEffect<PlantDestroySeeds>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-plant-seeds-remove", ("chance", Probability));
}
