using Content.Shared.Audio;
using Content.Shared.GameObjects.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedSteppedOnSoundSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        /// <summary>
        /// In seconds how long do we have to wait for another sound.
        /// </summary>
        private const float StepCooldown = 1.0f;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SteppedOnSoundComponent, SteppedOnEvent>(HandleSteppedOn);
        }

        private void HandleSteppedOn(EntityUid uid, SteppedOnSoundComponent component, SteppedOnEvent args)
        {
            if (!CanPlaySound(uid)) return;

            var currentTime = _gameTiming.CurTime;

            if (component.LastStep.TotalSeconds + StepCooldown > currentTime.TotalSeconds) return;

            component.LastStep = currentTime;
            SoundSystem.Play(GetFilter(component.Owner), _robustRandom.Pick(component.SoundCollection.PickFiles), component.Owner, AudioHelpers.WithVariation(0.01f));
        }

        protected virtual bool CanPlaySound(EntityUid uid)
        {
            return true;
        }

        // Client can predict its own stepping on sounds.
        protected abstract Filter GetFilter(IEntity entity);
    }
}
