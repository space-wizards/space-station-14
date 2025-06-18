using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

/// <summary>
///     Handles restoral of seeds on a plant.
/// </summary>
public sealed partial class PlantRestoreSeeds : EventEntityEffect<PlantRestoreSeeds>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-plant-seeds-add", ("chance", Probability));
}
