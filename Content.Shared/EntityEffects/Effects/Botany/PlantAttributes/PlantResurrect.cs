using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
///     Resurrects a dead plant
/// </summary>
public sealed partial class PlantResurrect : EntityEffectBase<PlantResurrect>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-plant-resurrect", ("chance", Probability));
}
