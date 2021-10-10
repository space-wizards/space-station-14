using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Shared.Jittering
{
    /// <summary>
    ///     A system for applying a jitter animation to any entity.
    /// </summary>
    public abstract class SharedJitteringSystem : EntitySystem
    {
        [Dependency] protected readonly IGameTiming GameTiming = default!;

        public float MaxAmplitude = 300f;
        public float MinAmplitude = 1f;

        public float MaxFrequency = 10f;
        public float MinFrequency = 1f;

        /// <summary>
        ///     List of jitter components to be removed, cached so we don't allocate it every tick.
        /// </summary>
        private readonly List<JitteringComponent> _removeList = new();

        public override void Initialize()
        {
            SubscribeLocalEvent<JitteringComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<JitteringComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnGetState(EntityUid uid, JitteringComponent component, ref ComponentGetState args)
        {
            args.State = new JitteringComponentState(component.EndTime, component.Amplitude, component.Frequency);
        }

        private void OnHandleState(EntityUid uid, JitteringComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not JitteringComponentState jitteringState)
                return;

            component.EndTime = jitteringState.EndTime;
            component.Amplitude = jitteringState.Amplitude;
            component.Frequency = jitteringState.Frequency;
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
        /// <param name="amplitude">Jitteriness of the animation. See <see cref="MaxAmplitude"/> and <see cref="MinAmplitude"/>.</param>
        /// <param name="frequency">Frequency for jittering. See <see cref="MaxFrequency"/> and <see cref="MinFrequency"/>.</param>
        /// <param name="forceValueChange">Whether to change any existing jitter value even if they're greater than the ones we're setting.</param>
        public void DoJitter(EntityUid uid, TimeSpan time, float amplitude = 10f, float frequency = 4f, bool forceValueChange = false)
        {
            var jittering = EntityManager.EnsureComponent<JitteringComponent>(uid);

            var endTime = GameTiming.CurTime + time;

            amplitude = Math.Clamp(amplitude, MinAmplitude, MaxAmplitude);
            frequency = Math.Clamp(frequency, MinFrequency, MaxFrequency);

            if (forceValueChange || jittering.EndTime < endTime)
                jittering.EndTime = endTime;

            if(forceValueChange || jittering.Amplitude < amplitude)
                jittering.Amplitude = amplitude;

            if (forceValueChange || jittering.Frequency < frequency)
                jittering.Frequency = frequency;

            jittering.Dirty();
        }

        /// <summary>
        ///     Immediately stops any jitter animation from an entity.
        /// </summary>
        /// <param name="uid">The entity in question.</param>
        public void StopJitter(EntityUid uid)
        {
            if (!EntityManager.HasComponent<JitteringComponent>(uid))
                return;

            EntityManager.RemoveComponent<JitteringComponent>(uid);
        }

        public override void Update(float frameTime)
        {
            foreach (var jittering in EntityManager.EntityQuery<JitteringComponent>())
            {
                if(jittering.EndTime <= GameTiming.CurTime)
                    _removeList.Add(jittering);
            }

            if (_removeList.Count == 0)
                return;

            foreach (var jittering in _removeList)
            {
                jittering.Owner.RemoveComponent<JitteringComponent>();
            }

            _removeList.Clear();
        }
    }
}
