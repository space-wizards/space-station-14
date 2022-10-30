using Robust.Shared.Audio;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Shared.Speech;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Server.Speech
{
    public sealed class SpeechSoundSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPrototypeManager _protoManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

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
            SoundSpecifier contextSound;
            var prototype = _protoManager.Index<SpeechSoundsPrototype>(component.SpeechSounds);
            var message = args.Message;

            // Different sounds for ask/exclaim based on last character
            switch (args.Message[^1])
            {
                case '?':
                    contextSound = prototype.AskSound;
                    break;
                case '!':
                    contextSound = prototype.ExclaimSound;
                    break;
                default:
                    contextSound = prototype.SaySound;
                    break;
            }

            // Use exclaim sound if most characters are uppercase.
            int uppercaseCount = 0;
            for (int i = 0; i < message.Length; i++)
            {
                if (char.IsUpper(message[i])) uppercaseCount++;
            }
            if (uppercaseCount > (message.Length / 2))
            {
                contextSound = contextSound = prototype.ExclaimSound;
            }

            var scale = (float) _random.NextGaussian(1, prototype.Variation);
            var pitchedAudioParams = component.AudioParams.WithPitchScale(scale);

            component.LastTimeSoundPlayed = currentTime;
            _audioSystem.Play(contextSound, Filter.Pvs(uid, entityManager: EntityManager), uid, pitchedAudioParams);
        }
    }
}
