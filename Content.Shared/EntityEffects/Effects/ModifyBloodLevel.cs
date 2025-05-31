using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class ModifyBloodLevel : EventEntityEffect<ModifyBloodLevel>
{
    [DataField]
    public bool Scaled = false;

    [DataField]
    public FixedPoint2 Amount = 1.0f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-modify-blood-level", ("chance", Probability),
            ("deltasign", MathF.Sign(Amount.Float())));
}
