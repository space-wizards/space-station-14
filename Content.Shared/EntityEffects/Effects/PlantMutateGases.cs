using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
///     changes the gases that a plant or produce create.
/// </summary>
public sealed partial class PlantMutateExudeGasses : EntityEffect
{
    [DataField]
    public float MinValue = 0.01f;

    [DataField]
    public float MaxValue = 0.5f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        // This is handled in EntityEffectSystem.cs
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-plant-mutate-exude-gasses",
            ("minValue", MinValue),
            ("maxValue", MaxValue));
    }
}

/// <summary>
///     changes the gases that a plant or produce consumes.
/// </summary>
public sealed partial class PlantMutateConsumeGasses : EntityEffect
{
    [DataField]
    public float MinValue = 0.01f;

    [DataField]
    public float MaxValue = 0.5f;

    public override void Effect(EntityEffectBaseArgs args)
    {
        // This is handled in EntityEffectSystem.cs
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-plant-mutate-consume-gasses",
            ("minValue", MinValue),
            ("maxValue", MaxValue));
    }
}
