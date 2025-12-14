using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Botany;

/// <summary>
/// See serverside system.
/// </summary>
public sealed partial class PlantMutateConsumeGases : EntityEffectBase<PlantMutateConsumeGases>
{
    [DataField]
    public float MinValue = 0.01f;

    [DataField]
    public float MaxValue = 0.5f;

    /// <inheritdoc/>
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("entity-effect-guidebook-plant-mutate-consume-gasses",
                ("chance", Probability),
                ("minValue", MinValue),
                ("maxValue", MaxValue));
    }
}

public sealed partial class PlantMutateExudeGases : EntityEffectBase<PlantMutateExudeGases>
{
    [DataField]
    public float MinValue = 0.01f;

    [DataField]
    public float MaxValue = 0.5f;

    /// <inheritdoc/>
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("entity-effect-guidebook-plant-mutate-exude-gasses",
                ("chance", Probability),
                ("minValue", MinValue),
                ("maxValue", MaxValue));
    }
}
