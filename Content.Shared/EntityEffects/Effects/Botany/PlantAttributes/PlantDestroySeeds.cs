using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
///     Handles removal of seeds on a plant.
/// </summary>
public sealed partial class PlantDestroySeeds : EntityEffectBase<PlantDestroySeeds>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-plant-seeds-remove", ("chance", Probability));
}
