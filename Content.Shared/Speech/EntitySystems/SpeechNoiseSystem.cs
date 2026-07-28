using Content.Shared.Chat;
using Content.Shared.Random.Helpers;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class SpeechSoundSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    [SubscribeLocalEvent]
    private void OnEntitySpoke(Entity<SpeechComponent> ent, ref EntitySpokeEvent args)
    {
        if (ent.Comp.SpeechSounds == null)
            return;

        var currentTime = _gameTiming.CurTime;
        var cooldown = TimeSpan.FromSeconds(ent.Comp.SoundCooldownTime);

        // Ensure more than the cooldown time has passed since last speaking
        if (currentTime - ent.Comp.LastTimeSoundPlayed < cooldown)
            return;

        var sound = GetSpeechSound(ent, args.Message);
        ent.Comp.LastTimeSoundPlayed = currentTime;
        _audio.PlayPredicted(sound, ent, ent);
    }

    /// <summary>
    /// Gets the speech sound for a message.
    /// </summary>
    public SoundSpecifier? GetSpeechSound(Entity<SpeechComponent> ent, string message)
    {
        if (ent.Comp.SpeechSounds == null)
            return null;

        // Play speech sound
        var prototype = ProtoMan.Index<SpeechSoundsPrototype>(ent.Comp.SpeechSounds);

        // Different sounds for ask/exclaim based on last character
        var contextSound = message[^1] switch
        {
            '?' => prototype.AskSound,
            '!' => prototype.ExclaimSound,
            _ => prototype.SaySound
        };

        // Use exclaim sound if most characters are uppercase.
        var uppercaseCount = 0;
        foreach (var t in message)
        {
            if (char.IsUpper(t))
                uppercaseCount++;
        }

        if (uppercaseCount > message.Length / 2)
        {
            contextSound = prototype.ExclaimSound;
        }

        var random = SharedRandomExtensions.PredictedRandom(_gameTiming, GetNetEntity(ent));
        var scale = (float)random.NextGaussian(1, prototype.Variation);
        contextSound.Params = ent.Comp.AudioParams.WithPitchScale(scale);
        return contextSound;
    }
}
