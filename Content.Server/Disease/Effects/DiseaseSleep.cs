using JetBrains.Annotations;
using Content.Shared.Disease;
using Content.Server.Bed.Sleep;

namespace Content.Server.Disease.Effects
{
    /// <summary>
    /// Forces you to sleep.
    /// </summary>
    [UsedImplicitly]
    public sealed class DiseaseSleep : DiseaseEffect
    {
        /// <summary>
        /// Set to 0 to force people asleep but without making them unable to be awoken.
        /// </summary>
        [DataField("forcedSecondsPerTick")]
        public float ForcedSecondsPerTick = 1.25f;

        public override void Effect(DiseaseEffectArgs args)
        {
            var sleepSys = args.EntityManager.EntitySysManager.GetEntitySystem<SleepingSystem>();

            sleepSys.AddForcedSleepingTime(args.DiseasedEntity, ForcedSecondsPerTick);
        }
    }
}
