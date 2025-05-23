using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Reset narcolepsy timer
/// </summary>
public sealed partial class ResetNarcolepsy : EventEntityEffect<ResetNarcolepsy>
{
    /// <summary>
    /// The # of seconds the effect resets the narcolepsy timer to
    /// </summary>
    [DataField("TimerReset")]
    public int TimerReset = 600;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-reset-narcolepsy", ("chance", Probability));
}
