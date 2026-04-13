using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;

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

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<JitteringStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusApplied);
            SubscribeLocalEvent<JitteringStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusRemoved);
        }

        private void OnStatusApplied(Entity<JitteringStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
        {
            if (!TryJitterFromStatuses(args.Target, out var setting))
                return;

            ApplyJitter(args.Target, setting);
        }

        private void OnStatusRemoved(Entity<JitteringStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
        {
            if (TryJitterFromStatuses(args.Target, out var setting))
                ApplyJitter(args.Target, setting);

            else
                RemCompDeferred<JitteringComponent>(args.Target);
        }

        public void ApplyJitter(EntityUid target, JitterSetting jitter)
        {
            var comp = EnsureComp<JitteringComponent>(target);
            comp.Settings = jitter;
            Dirty(target, comp);
        }

        public void EndJitter(EntityUid target)
        {

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

        /// <summary>
        /// Finds all the status effects with <see cref="JitteringStatusEffectComponent"/>
        /// and combines them into a single jitter effect.
        /// </summary>
        protected bool TryJitterFromStatuses(EntityUid entity, out JitterSetting jitter)
        {
            jitter = new JitterSetting();

            if (!_statusEffects.TryEffectsWithComp<JitteringStatusEffectComponent>(entity, out var effects))
                return false;

            foreach (var (_, jitterEffect, _) in effects)
            {
                jitter.Frequency += jitterEffect.Settings.Frequency;
                jitter.MaxRadius += jitterEffect.Settings.MaxRadius;
                jitter.MinRadius += jitterEffect.Settings.MinRadius;
                // todo Do this properly by serializing Matrix2x3
                var combinedMatrix = jitter.Matrix * jitterEffect.Settings.Matrix;
                jitter.XSheer = combinedMatrix.X;
                jitter.YSheer = combinedMatrix.Y;
            }

            jitter.Frequency /= effects.Count;
            jitter.MaxRadius /= effects.Count;
            jitter.MinRadius /= effects.Count;

            return true;
        }
    }
}
