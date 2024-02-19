using System.Linq;
using Robust.Shared.Audio;
using Content.Server.Chat.V2;
using Content.Shared.Speech;
using Robust.Shared.Audio.Systems;
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
        [Dependency] private readonly SharedAudioSystem _audio = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpeechComponent, LocalChatSuccessEvent>(OnEntitySpoke);
        }

        public SoundSpecifier? GetSpeechSound(Entity<SpeechComponent> ent, string message)
        {
            if (ent.Comp.SpeechSounds == null)
                return null;

            // Play speech sound
            var prototype = _protoManager.Index<SpeechSoundsPrototype>(ent.Comp.SpeechSounds);

            // Different sounds for ask/exclaim based on last character
            var contextSound = message[^1] switch
            {
                '?' => prototype.AskSound,
                '!' => prototype.ExclaimSound,
                _ => prototype.SaySound
            };

            // Use exclaim sound if most characters are uppercase.
            if (message.Count(char.IsUpper) > (message.Length / 2))
            {
                contextSound = prototype.ExclaimSound;
            }

            contextSound.Params = ent.Comp.AudioParams.WithPitchScale((float) _random.NextGaussian(1, prototype.Variation));

            return contextSound;
        }

        private void OnEntitySpoke(EntityUid uid, SpeechComponent component, LocalChatSuccessEvent args)
        {
            if (component.SpeechSounds == null)
                return;

            var currentTime = _gameTiming.CurTime;
            var cooldown = TimeSpan.FromSeconds(component.SoundCooldownTime);

            // Ensure more than the cooldown time has passed since last speaking
            if (currentTime - component.LastTimeSoundPlayed < cooldown)
                return;

            var sound = GetSpeechSound((uid, component), args.Message);
            component.LastTimeSoundPlayed = currentTime;
            _audio.PlayPvs(sound, uid);
        }
    }
}
