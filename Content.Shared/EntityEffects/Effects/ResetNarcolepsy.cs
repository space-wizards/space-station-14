using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Reset narcolepsy timer
/// </summary>
public sealed partial class ResetNarcolepsy : EventEntityEffect<ResetNarcolepsy>
{
    /// <summary>
    /// The time the effect sets the narcolepsy timer to.
    /// </summary>
    [DataField("TimerReset")]
    public TimeSpan TimerReset = TimeSpan.FromMinutes(10);

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-reset-narcolepsy", ("chance", Probability));
}
