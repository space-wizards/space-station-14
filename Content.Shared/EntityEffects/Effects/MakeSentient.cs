using Content.Shared.Mind.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class MakeSentient : EventEntityEffect<MakeSentient>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-make-sentient", ("chance", Probability));
}
