using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

// todo big picture
// check for stacking jitters in new status effects

// todo fix namespace
namespace Content.Shared.Jittering
{

    /// <summary>
    /// A system for applying a jitter animation to any entity.
    /// </summary>
    public abstract class SharedJitteringSystem : EntitySystem
    {
        [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

        // This prototype exists as a compatibility layer with previous jittering.
        // Ideally nothing calls `CreateJitter` but instead goes through status effects
        private static readonly EntProtoId BasicJitter = "StatusEffectStandardJitter";

        public void CreateJitter(EntityUid target, JitterSetting jitter, TimeSpan duration)
        {
            if (!_statusEffects.TryAddStatusEffectDuration(target, BasicJitter, out var statusComp, duration))
                return;

            var jitterComp = EnsureComp<JitteringStatusEffectComponent>(statusComp.Value);
            jitterComp.Settings = jitter;
        }

        // todo make remark clearer
        /// <summary>
        /// Applies a jitter effect to the specified entity.
        /// </summary>
        /// <remarks>
        /// If the entity is already jittering, the jitter values will be updated but only if they're greater
        /// than the current ones and <see cref="forceValueChange"/> is false.
        /// </remarks>
        /// <param name="uid">Entity in question.</param>
        /// <param name="time">For how much time to apply the effect.</param>
        /// <param name="refresh">The status effect cooldown should be refreshed (true) or accumulated (false).</param>
        /// <param name="amplitude">Jitteriness of the animation. See <see cref="MaxAmplitude"/> and <see cref="MinAmplitude"/>.</param>
        /// <param name="frequency">Frequency for jittering. See <see cref="MaxFrequency"/> and <see cref="MinFrequency"/>.</param>
        /// <param name="forceValueChange">Whether to change any existing jitter value even if they're greater than the ones we're setting.</param>
        /// <param name="status">The status effects component to modify.</param>
        public void DoJitter(EntityUid uid,
                            TimeSpan time,
                            bool refresh,
                            float amplitude = 10f,
                            float frequency = 4f,
                            bool forceValueChange = false)
        // public void DoJitter(EntityUid uid,
        // TimeSpan time,
        // bool refresh,
        // float amplitude = 10f,
        // float frequency = 4f,
        // bool forceValueChange = false,
        //     StatusEffectsComponent? status = null)
        {
            // if (!Resolve(uid, ref status, false))
            //     return;
            //
            // // todo
            // if (_statusEffects.TryAddStatusEffect<JitteringComponent>(uid, "Jitter", time, refresh, status))
            // {
            //     var jittering = Comp<JitteringComponent>(uid);
            //
            //     // if(forceValueChange || jittering.Amplitude < amplitude)
            //     //     jittering.Amplitude = amplitude;
            //     //
            //     // if (forceValueChange || jittering.Frequency < frequency)
            //     //     jittering.Frequency = frequency;
            // }
        }

        // todo Shouldn't need this with new status effects
        /// <summary>
        /// For non mobs.
        /// </summary>
        public void AddJitter(EntityUid uid, float amplitude = 10f, float frequency = 4f)
        {
            // var jitter = EnsureComp<JitteringComponent>(uid);
            // jitter.MaxRadius = amplitude; //todo
            // jitter.Frequency = frequency;
            // Dirty(uid, jitter);
        }
    }
}
