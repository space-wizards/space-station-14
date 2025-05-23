using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Reset narcolepsy timer
/// </summary>
[UsedImplicitly]
public sealed partial class ResetNarcolepsy : EntityEffect
{
    /// <summary>
    /// The # of seconds the effect resets the narcolepsy timer to
    /// </summary>
    [DataField("TimerReset")]
    public int TimerReset = 600;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-reset-narcolepsy", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var evt = new ExecuteEntityEffectEvent<ResetNarcolepsy>(this, args);
        args.EntityManager.EventBus.RaiseEvent(EventSource.Local, ref evt);
    }
}
