using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantDiethylamine : EntityEffectBase<PlantDiethylamine>
{
    /// <inheritdoc/>
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys, ILocalizationManager loc) =>
        loc.GetString("entity-effect-guidebook-plant-diethylamine", ("chance", Probability));
}

