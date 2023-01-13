using Content.Server.Traits.Assorted;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects;

/// <summary>
/// Reset narcolepsy timer
/// </summary>
[UsedImplicitly]
public sealed class ResetNarcolepsy : ReagentEffect
{
    /// <summary>
    /// The # of seconds the effect resets the narcolepsy timer to
    /// </summary>
    [DataField("TimerReset")]
    public int TimerReset = 600;

    public override void Effect(ReagentEffectArgs args)
    {
        if (args.Scale != 1f)
            return;

        args.EntityManager.EntitySysManager.GetEntitySystem<NarcolepsySystem>().AdjustNarcolepsyTimer(args.SolutionEntity, TimerReset);
    }
}
