using Content.Shared.Database;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class FlammableReaction : EventEntityEffect<FlammableReaction>
{
    [DataField]
    public float Multiplier = 0.05f;

    // The fire stack multiplier if fire stacks already exist on target, only works if 0 or greater
    [DataField]
    public float MultiplierOnExisting = -1f;

    public override bool ShouldLog => true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-flammable-reaction", ("chance", Probability));

    public override LogImpact LogImpact => LogImpact.Medium;
}
