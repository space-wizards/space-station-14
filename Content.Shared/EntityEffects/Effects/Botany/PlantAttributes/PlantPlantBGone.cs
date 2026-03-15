using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantPlantBGone : EntityEffectBase<PlantPlantBGone>
{
    /// <inheritdoc/>
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-plant-plantbgone", ("chance", Probability));
}
