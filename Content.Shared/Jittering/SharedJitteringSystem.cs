using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffect;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Jittering
{
    /// <summary>
    ///     A system for applying a jitter animation to any entity.
    /// </summary>
    public abstract class SharedJitteringSystem : EntitySystem
    {
        [Dependency] protected readonly IGameTiming GameTiming = default!;
        [Dependency] protected readonly StatusEffectsSystem StatusEffects = default!;

        public float MaxAmplitude = 300f;
        public float MinAmplitude = 1f;

        public float MaxFrequency = 10f;
        public float MinFrequency = 1f;

        public override void Initialize()
        {
            SubscribeLocalEvent<JitteringComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<JitteringComponent, ComponentHandleState>(OnHandleState);
            SubscribeLocalEvent<JitteringComponent, RejuvenateEvent>(OnRejuvenate);
        }

        private void OnGetState(EntityUid uid, JitteringComponent component, ref ComponentGetState args)
        {
            args.State = new JitteringComponentState(component.Amplitude, component.Frequency);
        }

        private void OnHandleState(EntityUid uid, JitteringComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not JitteringComponentState jitteringState)
                return;

            component.Amplitude = jitteringState.Amplitude;
            component.Frequency = jitteringState.Frequency;
        }

        private void OnRejuvenate(EntityUid uid, JitteringComponent component, RejuvenateEvent args)
        {
            EntityManager.RemoveComponentDeferred<JitteringComponent>(uid);
        }

        /// <summary>
        ///     Applies a jitter effect to the specified entity.
        ///     You can apply this to any entity whatsoever, so be careful what you use it on!
        /// </summary>
        /// <remarks>
        ///     If the entity is already jittering, the jitter values will be updated but only if they're greater
        ///     than the current ones and <see cref="forceValueChange"/> is false.
        /// </remarks>
        /// <param name="uid">Entity in question.</param>
        /// <param name="time">For how much time to apply the effect.</param>
        /// <param name="refresh">The status effect cooldown should be refreshed (true) or accumulated (false).</param>
        /// <param name="amplitude">Jitteriness of the animation. See <see cref="MaxAmplitude"/> and <see cref="MinAmplitude"/>.</param>
        /// <param name="frequency">Frequency for jittering. See <see cref="MaxFrequency"/> and <see cref="MinFrequency"/>.</param>
        /// <param name="forceValueChange">Whether to change any existing jitter value even if they're greater than the ones we're setting.</param>
        /// <param name="status">The status effects component to modify.</param>
        public void DoJitter(EntityUid uid, TimeSpan time, bool refresh, float amplitude = 10f, float frequency = 4f, bool forceValueChange = false,
            StatusEffectsComponent? status = null)
        {
            if (!Resolve(uid, ref status, false))
                return;

            amplitude = Math.Clamp(amplitude, MinAmplitude, MaxAmplitude);
            frequency = Math.Clamp(frequency, MinFrequency, MaxFrequency);

            if (StatusEffects.TryAddStatusEffect<JitteringComponent>(uid, "Jitter", time, refresh, status))
            {
                var jittering = EntityManager.GetComponent<JitteringComponent>(uid);

                if(forceValueChange || jittering.Amplitude < amplitude)
                    jittering.Amplitude = amplitude;

                if (forceValueChange || jittering.Frequency < frequency)
                    jittering.Frequency = frequency;
            }
        }

        /// <summary>
        /// For non mobs.
        /// </summary>
        public void AddJitter(EntityUid uid, float amplitude = 10f, float frequency = 4f)
        {
            var jitter = EnsureComp<JitteringComponent>(uid);
            jitter.Amplitude = amplitude;
            jitter.Frequency = frequency;
            Dirty(jitter);
        }
    }
}
