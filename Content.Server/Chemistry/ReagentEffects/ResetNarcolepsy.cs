using Content.Server.Traits.Assorted;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

/// <summary>
/// Reset narcolepsy timer
/// </summary>
[UsedImplicitly]
public sealed partial class ResetNarcolepsy : ReagentEffect
{
    /// <summary>
    /// The # of seconds the effect resets the narcolepsy timer to
    /// </summary>
    [DataField("TimerReset")]
    public int TimerReset = 600;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-reset-narcolepsy", ("chance", Probability));

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.Scale != 1f)
            return;

        args.EntityManager.EntitySysManager.GetEntitySystem<NarcolepsySystem>().AdjustNarcolepsyTimer(args.SolutionEntity, TimerReset);
    }
}
