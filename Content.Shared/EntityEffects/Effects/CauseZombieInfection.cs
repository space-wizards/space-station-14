using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class CauseZombieInfection : EventEntityEffect<CauseZombieInfection>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cause-zombie-infection", ("chance", Probability));
}
