using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

/// <summary>
///     Resurrects a dead plant
/// </summary>
public sealed partial class PlantResurrect : EntityEffectBase<PlantResurrect>
{
    /// <summary>
    /// Whether reviving the plant will remove its seeds, default true
    /// </summary>
    [DataField]
    public bool ReviveSeedless = true;
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-plant-resurrect", ("chance", Probability), ("seedless", ReviveSeedless));
}
