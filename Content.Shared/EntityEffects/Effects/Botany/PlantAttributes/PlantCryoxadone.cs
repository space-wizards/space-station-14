using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantCryoxadone : EntityEffectBase<PlantCryoxadone>
{
    protected override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-plant-cryoxadone", ("chance", Probability));
}
