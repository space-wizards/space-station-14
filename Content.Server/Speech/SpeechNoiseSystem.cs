using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Content.Server.Chat;
using Content.Shared.Speech;
using Content.Shared.Sound;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System;

namespace Content.Server.Speech
{
    public sealed class SpeechSoundSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedSpeechComponent, EntitySpokeEvent>(OnEntitySpoke);
        }

        private void OnEntitySpoke(EntityUid uid, SharedSpeechComponent component, EntitySpokeEvent args)
        {
            if (component.SpeechSounds == null) return;

            var currentTime = _gameTiming.CurTime;
            var cooldown = TimeSpan.FromSeconds(component.SoundCooldownTime);

            // Ensure more than the cooldown time has passed since last speaking
            if (currentTime - component.LastTimeSoundPlayed < cooldown) return;

            // Play speech sound
            string contextSound;
            var prototype = _protoManager.Index<SpeechSoundsPrototype>(component.SpeechSounds);

            // Different sounds for ask/exclaim based on last character
            switch (args.Message[^1])
            {
                case '?':
                    contextSound = prototype.AskSound.GetSound();
                    break;
                case '!':
                    contextSound = prototype.ExclaimSound.GetSound();
                    break;
                default:
                    contextSound = prototype.SaySound.GetSound();
                    break;
            }

            component.LastTimeSoundPlayed = currentTime;
            SoundSystem.Play(Filter.Pvs(uid, entityManager: EntityManager), contextSound, uid, component.AudioParams);
        }
    }
}
