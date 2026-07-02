using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class RobustHarvest : EntityEffect
{
    /// <summary>
    /// How high potency can go.
    /// </summary>
    [DataField]
    public int PotencyLimit = 50;

    /// <summary>
    /// The increase in potency per effect application.
    /// </summary>
    [DataField]
    public int PotencyIncrease = 3;

    /// <summary>
    /// If potency passes this threshold, the produce will not have any seeds.
    /// </summary>
    [DataField]
    public int PotencySeedlessThreshold = 30;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-plant-robust-harvest",
            ("seedlesstreshold", PotencySeedlessThreshold),
            ("limit", PotencyLimit),
            ("increase", PotencyIncrease),
            ("chance", Probability));
}
