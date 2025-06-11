using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class ModifyBleedAmount : EventEntityEffect<ModifyBleedAmount>
{
    [DataField]
    public bool Scaled = false;

    [DataField]
    public float Amount = -1.0f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-modify-bleed-amount", ("chance", Probability),
            ("deltasign", MathF.Sign(Amount)));
}
