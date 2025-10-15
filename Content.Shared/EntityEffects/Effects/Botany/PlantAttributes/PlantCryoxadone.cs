using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantCryoxadone : EntityEffectBase<PlantCryoxadone>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys, ILocalizationManager loc) =>
        loc.GetString("entity-effect-guidebook-plant-cryoxadone", ("chance", Probability));
}
