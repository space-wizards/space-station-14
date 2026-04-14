using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

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
        // Ideally nothing calls `CreateJitter` but instead goes through status effects in their own way
        private static readonly EntProtoId BasicJitter = "StatusEffectStandardJitter";

        // This value approximates the old formula used to translate "amplitude" into a coordinate position
        private const float AmplitudeScalar = 0.01375f;

        /// <summary>
        /// Creates a new status effect on an entity that causes its sprite to move erratically.
        /// </summary>
        /// <param name="target">The entity that will begin jittering.</param>
        /// <param name="jitter">What kind of jitter to apply.</param>
        /// <param name="duration">How long the entity should jitter.</param>
        [Obsolete("Jittering should be applied by a bespoke effect from StatusEffectsSystem.")]
        public void CreateJitter(EntityUid target, JitterParameters jitter, TimeSpan duration)
        {
            // todo fix duration
            if (!_statusEffects.TryUpdateStatusEffectDuration(target, BasicJitter, out var statusEnt, duration))
                return;

            var jitterComp = EnsureComp<JitteringStatusEffectComponent>(statusEnt.Value);
            jitterComp.Jitter = jitter;
            Dirty(statusEnt.Value, jitterComp);
        }

        // todo
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
        [Obsolete("Jittering should be applied by a bespoke effect from StatusEffectsSystem.")]
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
            var jitter = new JitterParameters()
            {
                Frequency = frequency,
                MinRadius = amplitude * AmplitudeScalar / 2,
                MaxRadius = amplitude * AmplitudeScalar,
            };

            CreateJitter(uid, jitter, time);
        }

        // todo
        /// <summary>
        /// For non mobs.
        /// </summary>
        [Obsolete("Jittering should be applied by a bespoke effect from StatusEffectsSystem.")]
        public void AddJitter(EntityUid uid, float amplitude = 10f, float frequency = 4f)
        {
            var jitter = new JitterParameters()
            {
                Frequency = frequency,
                MinRadius = amplitude * AmplitudeScalar / 2,
                MaxRadius = amplitude * AmplitudeScalar,
            };

            CreateJitter(uid, jitter, TimeSpan.Zero); // todo
        }
    }
}
