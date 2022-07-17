using Content.Shared.Chemistry.Reagent;
using Content.Server.Disease;
using Content.Server.Bed.Sleep;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Puts the solution entity to sleep.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemSleep : ReagentEffect
    {
        [DataField("forcedSecondsPerTick")]
        public float ForcedSecondsPerTick = 1.25f;

        /// <summary>
        /// Set to 0 to force people asleep but without making them unable to be awoken.
        /// </summary>
        public override void Effect(ReagentEffectArgs args)
        {
            EntitySystem.Get<SleepingSystem>().AddForcedSleepingTime(args.SolutionEntity, ForcedSecondsPerTick);
        }
    }
}
