using Content.Server.Chat.Systems;
using Content.Shared.Speech;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SurveillanceCamera;

/// <summary>
///     This handles speech for surveillance camera monitors.
/// </summary>
public sealed class SurveillanceCameraSpeakerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SurveillanceCameraSpeakerComponent, SurveillanceCameraSpeechSendEvent>(OnSpeechSent);
    }

    private void OnSpeechSent(EntityUid uid, SurveillanceCameraSpeakerComponent component,
        SurveillanceCameraSpeechSendEvent args)
    {
        if (!component.SpeechEnabled)
        {
            return;
        }

        var time = _gameTiming.CurTime;
        var cd = TimeSpan.FromSeconds(component.SpeechSoundCooldown);

        // this part's mostly copied from speech
        if (time - component.LastSoundPlayed < cd
            && TryComp<SpeechComponent>(args.Speaker, out var speech)
            && speech.SpeechSounds != null
            && _prototypeManager.TryIndex(speech.SpeechSounds, out SpeechSoundsPrototype? speechProto))
        {
            var sound = args.Message[^1] switch
            {
                '?' => speechProto.AskSound,
                '!' => speechProto.ExclaimSound,
                _ => speechProto.SaySound
            };

            var uppercase = 0;
            for (var i = 0; i < args.Message.Length; i++)
            {
                if (char.IsUpper(args.Message[i]))
                {
                    uppercase++;
                }
            }

            if (uppercase > args.Message.Length / 2)
            {
                sound = speechProto.ExclaimSound;
            }

            var scale = (float) _random.NextGaussian(1, speechProto.Variation);
            var param = speech.AudioParams.WithPitchScale(scale);
            _audioSystem.PlayPvs(sound, uid, param);

            component.LastSoundPlayed = time;
        }

        var nameEv = new TransformSpeakerNameEvent(args.Speaker, Name(args.Speaker));
        RaiseLocalEvent(args.Speaker, nameEv);

        var name = Loc.GetString("speech-name-relay", ("speaker", Name(uid)),
            ("originalName", nameEv.Name));

        // log to chat so people can identity the speaker/source, but avoid clogging ghost chat if there are many radios
        _chatSystem.TrySendInGameICMessage(uid, args.Message, InGameICChatType.Speak, ChatTransmitRange.GhostRangeLimit, nameOverride: name);
    }
}
