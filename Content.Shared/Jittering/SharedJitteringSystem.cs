using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

// todo fix namespace
namespace Content.Shared.Jittering
{
    /// <summary>
    /// Handles "jitter" animations where a sprite moves around a point erratically.
    /// </summary>
    public abstract class SharedJitteringSystem : EntitySystem
    {
        [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

        // This prototype exists as a compatibility layer with previous jittering.
        // Ideally nothing calls `CreateJitter` but instead goes through status effects in their own way
        private static readonly EntProtoId BasicJitter = "StatusEffectBasicJitter";

        // This value approximates the old formula used to translate "amplitude" into a coordinate position
        private const float AmplitudeScalar = 0.01375f;

        /// <summary>
        /// Creates a new status effect on an entity that causes its sprite to move erratically.
        /// </summary>
        /// <param name="target">The entity that will begin jittering.</param>
        /// <param name="jitter">What kind of jitter to apply.</param>
        /// <param name="duration">How long the entity should jitter.</param>
        [Obsolete("Jittering should be applied by a bespoke effect from StatusEffectsSystem.")]
        public void CreateJitter(EntityUid target, JitterParameters jitter, TimeSpan? duration)
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
        /// <param name="uid">Entity to start jittering.</param>
        /// <param name="time">For how much time to apply the effect.</param>
        /// <param name="refresh">The status effect cooldown should be refreshed (true) or accumulated (false).</param>
        /// <param name="amplitude">Distance the jitter travels.</param>
        /// <param name="frequency">Jitters per second.</param>
        /// <param name="forceValueChange">Whether to change any existing jitter value even if they're greater than the ones we're setting.</param>
        [Obsolete("Jittering should be applied by a bespoke effect from StatusEffectsSystem.")]
        public void DoJitter(EntityUid uid,
                            TimeSpan time,
                            bool refresh,
                            float amplitude = 10f,
                            float frequency = 4f,
                            bool forceValueChange = false)
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
