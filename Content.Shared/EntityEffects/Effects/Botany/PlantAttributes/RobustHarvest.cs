using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class RobustHarvest : EntityEffectBase<RobustHarvest>
{
    [DataField]
    public int PotencyLimit = 50;

    [DataField]
    public int PotencyIncrease = 3;

    [DataField]
    public int PotencySeedlessThreshold = 30;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys, ILocalizationManager loc) =>
        loc.GetString("entity-effect-guidebook-plant-robust-harvest",
            ("seedlesstreshold", PotencySeedlessThreshold),
            ("limit", PotencyLimit),
            ("increase", PotencyIncrease),
            ("chance", Probability));
}
