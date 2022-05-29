using Content.Shared.Chemistry.Reagent;
using Content.Shared.Jittering;

namespace Content.Server.Chemistry.ReagentEffects.StatusEffects
{
    /// <summary>
    ///     Adds the jitter status effect to a mob.
    ///     This doesn't use generic status effects because it needs to
    ///     take in some parameters that JitterSystem needs.
    /// </summary>
    public sealed class Jitter : ReagentEffect
    {
        [DataField("amplitude")]
        public float Amplitude = 10.0f;

        [DataField("frequency")]
        public float Frequency = 4.0f;

        [DataField("time")]
        public float Time = 2.0f;

        /// <remarks>
        ///     true - refresh jitter time,  false - accumulate jitter time
        /// </remarks>
        [DataField("refresh")]
        public bool Refresh = true;

        public override void Effect(ReagentEffectArgs args)
        {
            args.EntityManager.EntitySysManager.GetEntitySystem<SharedJitteringSystem>()
                .DoJitter(args.SolutionEntity, TimeSpan.FromSeconds(Time), Refresh, Amplitude, Frequency);
        }
    }
}
