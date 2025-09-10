using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class AdjustTemperature : EventEntityEffect<AdjustTemperature>
{
    [DataField]
    public float Amount;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-adjust-temperature",
            ("chance", Probability),
            ("deltasign", MathF.Sign(Amount)),
            ("amount", MathF.Abs(Amount)));
}
