using System.Threading.Tasks;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.CCVar;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.TTS;
using Content.Shared.GameTicking;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.DeadSpace.Languages;
using Content.Shared.DeadSpace.Languages.Prototypes;

namespace Content.Server.Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly TTSManager _ttsManager = default!;
    [Dependency] private readonly IRobustRandom _rng = default!;
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!; // DS14

    private readonly List<string> _sampleText =
        new()
        {
            "Съешь же ещё этих мягких французских булок, да выпей чаю.",
            "Клоун, прекрати разбрасывать банановые кожурки офицерам под ноги!",
            "Капитан, вы уверены что хотите назначить клоуна на должность главы персонала?",
            "Эс Бэ! Тут человек в сером костюме, с тулбоксом и в маске! Помогите!!",
            "Я надеюсь что инженеры внимательно следят за сингулярностью...",
            "Вы слышали эти странные крики в техах? Мне кажется туда ходить небезопасно.",
            "Вы не видели Гамлета? Мне кажется он забегал к вам на кухню.",
            "Здесь есть доктор? Человек умирает от отравленного пончика! Нужна помощь!",
            "Возле эвакуационного шаттла разгерметизация! Инженеры, нам срочно нужна ваша помощь!",
            "Бармен, налей мне самого крепкого вина, которое есть в твоих запасах!"
        };

    private const int MaxMessageChars = 100 * 3; // same as SingleBubbleCharLimit * 3
    private bool _isEnabled = false;

    public override void Initialize()
    {
        _cfg.OnValueChanged(CCCVars.TTSEnabled, v => _isEnabled = v, true);

        SubscribeLocalEvent<TransformSpeechEvent>(OnTransformSpeech);
        SubscribeLocalEvent<TTSComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<TTSComponent, EntitySpokeToEntityEvent>(OnEntitySpokeToEntity);
        SubscribeLocalEvent<RadioSpokeEvent>(OnRadioSpokeEvent);
        SubscribeLocalEvent<AnnounceSpokeEvent>(OnAnnounceSpokeEvent);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);

        RegisterRateLimits();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _ttsManager.ResetCache();
    }

    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled ||
            !_prototypeManager.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var protoVoice))
            return;

        if (HandleRateLimit(args.SenderSession) != RateLimitStatus.Allowed)
            return;

        var previewText = _rng.Pick(_sampleText);
        var soundData = await GenerateTTS(previewText, protoVoice.Speaker);
        if (soundData is null)
            return;

        RaiseNetworkEvent(new PlayTTSEvent(soundData), Filter.SinglePlayer(args.SenderSession));
    }

    private async void OnEntitySpoke(EntityUid uid, TTSComponent component, EntitySpokeEvent args)
    {
        var voiceId = component.VoicePrototypeId;
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars ||
            voiceId == null)
            return;

        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId);
        RaiseLocalEvent(uid, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
            return;

        if (args.ObfuscatedMessage != null)
        {
            HandleWhisper(uid, args.Message, args.LexiconMessage, args.LanguageId, args.ObfuscatedMessage, protoVoice.Speaker);
            return;
        }

        HandleSay(uid, args.Message, args.LexiconMessage, args.LanguageId, protoVoice.Speaker);
    }

    private async void OnEntitySpokeToEntity(EntityUid uid, TTSComponent component, EntitySpokeToEntityEvent args)
    {
        var voiceId = component.VoicePrototypeId;
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars ||
            voiceId == null)
            return;

        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId);
        RaiseLocalEvent(uid, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
            return;

        HandleDirectSay(args.Target, args.Message, args.LexiconMessage, args.LanguageId, protoVoice.Speaker);
    }

    private async void OnRadioSpokeEvent(RadioSpokeEvent args)
    {
        if (!_isEnabled ||
            args.Message.Length > MaxMessageChars)
            return;

        if (!TryComp(args.Source, out TTSComponent? component))
            return;

        var voiceId = component.VoicePrototypeId;

        if (voiceId == null)
            return;

        var voiceEv = new TransformSpeakerVoiceEvent(args.Source, voiceId);
        RaiseLocalEvent(args.Source, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
            return;

        HandleRadio(args.Receivers, args.Message, args.LexiconMessage, args.LanguageId, protoVoice.Speaker);
    }

    private async void OnAnnounceSpokeEvent(AnnounceSpokeEvent args)
    {
        var voiceId = args.Voice;
        if (!_isEnabled ||
            args.Message.Length > _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength) ||
            voiceId == null)
            return;

        if (args.Source != null)
        {
            var voiceEv = new TransformSpeakerVoiceEvent(args.Source.Value, voiceId);
            RaiseLocalEvent(args.Source.Value, voiceEv);
            voiceId = voiceEv.VoiceId;
        }

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var protoVoice))
            return;

        Timer.Spawn(6000, () => HandleAnnounce(args.Message, args.LexiconMessage, args.LanguageId, protoVoice.Speaker, args.Filter)); // Awful, but better than sending announce sound to client in resource file
    }

    private async void HandleSay(EntityUid uid, string message, string lexiconMessage, ProtoId<LanguagePrototype> languageId, string speaker)
    {
        // DS14-start
        var recipientData = GetExpandedVoiceRecipients(uid, SharedChatSystem.VoiceRange);
        var recipients = recipientData.Keys;
        // DS14-end
        var soundData = await GenerateTTS(message, speaker);

        byte[]? soundLexiconData = null;
        var understanding = new HashSet<ICommonSession>(_language.GetUnderstanding(languageId));

        if (NeedsLexiconTTS(languageId, recipients, understanding))
            soundLexiconData = await GenerateTTS(lexiconMessage, speaker);

        if (soundData is null) return;

        // DS14-start: carry recipient-specific remote hearing attenuation and source.
        foreach (var (session, data) in recipientData)
        {
            var audioSource = GetNetEntity(data.AudioSourceOverride ?? uid);

            if (!understanding.Contains(session))
            {
                if (soundLexiconData is null)
                    RaiseNetworkEvent(new PlayTTSEvent(new byte[0], audioSource, isSoundLexicon: true, languageId: languageId, distanceOverride: data.AudioRangeOverride), session);
                else
                    RaiseNetworkEvent(new PlayTTSEvent(soundLexiconData, audioSource, distanceOverride: data.AudioRangeOverride), session);
            }
            else
                RaiseNetworkEvent(new PlayTTSEvent(soundData, audioSource, isSoundLexicon: false, distanceOverride: data.AudioRangeOverride), session);
        }
        // DS14-end

    }

    private async void HandleDirectSay(EntityUid uid, string message, string lexiconMessage, ProtoId<LanguagePrototype> languageId, string speaker)
    {
        var soundData = await GenerateTTS(message, speaker);

        byte[]? soundLexiconData = null;

        if (_language.NeedGenerateDirectTTS(uid, languageId))
            soundLexiconData = await GenerateTTS(lexiconMessage, speaker);

        if (soundData is null) return;

        if (!_language.KnowsLanguage(uid, languageId))
        {
            if (soundLexiconData is null)
                RaiseNetworkEvent(new PlayTTSEvent(new byte[0], GetNetEntity(uid), isSoundLexicon: true, languageId: languageId), uid);
            else
                RaiseNetworkEvent(new PlayTTSEvent(soundLexiconData, GetNetEntity(uid)), uid);
        }
        else
            RaiseNetworkEvent(new PlayTTSEvent(soundData, GetNetEntity(uid)), uid);
    }

    private async void HandleRadio(EntityUid[] uids, string message, string lexiconMessage, ProtoId<LanguagePrototype> languageId, string speaker)
    {
        var soundData = await GenerateTTS(message, speaker);

        byte[]? soundLexiconData = null;

        if (_language.NeedGenerateRadioTTS(languageId, uids, out var understandings, out var notUnderstandings))
            soundLexiconData = await GenerateTTS(lexiconMessage, speaker);

        if (soundData is null) return;

        foreach (var uid in understandings)
        {
            RaiseNetworkEvent(new PlayTTSEvent(soundData, GetNetEntity(uid), isRadio: true), Filter.Entities(uid));
        }

        foreach (var uid in notUnderstandings)
        {
            if (soundLexiconData is null)
                RaiseNetworkEvent(new PlayTTSEvent(new byte[0], GetNetEntity(uid), isRadio: true, isSoundLexicon: true, languageId: languageId), Filter.Entities(uid));
            else
                RaiseNetworkEvent(new PlayTTSEvent(soundLexiconData, GetNetEntity(uid), isRadio: true), Filter.Entities(uid));
        }

    }

    private async void HandleAnnounce(string message, string lexiconMessage, ProtoId<LanguagePrototype> languageId, string speaker, Filter filter)
    {
        var soundData = await GenerateTTS(message, speaker);

        byte[]? soundLexiconData = null;
        List<ICommonSession> understanding = new List<ICommonSession>();

        if (_language.NeedGenerateFilterTTS(languageId, filter, out understanding))
            soundLexiconData = await GenerateTTS(lexiconMessage, speaker);

        if (soundData is null) return;

        foreach (var session in filter.Recipients)
        {
            if (!understanding.Contains(session))
            {
                if (soundLexiconData is null)
                    RaiseNetworkEvent(new PlayTTSEvent(new byte[0], isSoundLexicon: true, languageId: languageId), session);
                else
                    RaiseNetworkEvent(new PlayTTSEvent(soundLexiconData), session);
            }
            else
                RaiseNetworkEvent(new PlayTTSEvent(soundData), session);
        }
    }

    private async void HandleWhisper(EntityUid uid, string message, string lexiconMessage, ProtoId<LanguagePrototype> languageId, string obfMessage, string speaker)
    {
        // DS14-start
        var recipientData = GetExpandedVoiceRecipients(uid, SharedChatSystem.WhisperMuffledRange);
        var recipients = recipientData.Keys;
        // DS14-end
        var fullSoundData = await GenerateTTS(message, speaker, true);

        byte[]? lexiconSoundData = null;
        var understanding = new HashSet<ICommonSession>(_language.GetUnderstanding(languageId));

        if (NeedsLexiconTTS(languageId, recipients, understanding))
            lexiconSoundData = await GenerateTTS(lexiconMessage, speaker);

        // var obfSoundData = await GenerateTTS(obfMessage, speaker, true);
        // if (obfSoundData is null) return;
        // var obfTtsEvent = new PlayTTSEvent(obfSoundData, GetNetEntity(uid), true);

        if (fullSoundData is null) return;

        // DS14-start: carry recipient-specific remote hearing attenuation and source.
        foreach (var (session, data) in recipientData)
        {
            var audioSource = GetNetEntity(data.AudioSourceOverride ?? uid);

            if (!understanding.Contains(session))
            {
                if (lexiconSoundData is null)
                    RaiseNetworkEvent(new PlayTTSEvent(new byte[0], audioSource, isWhisper: true, isSoundLexicon: true, languageId: languageId, distanceOverride: data.AudioRangeOverride), session);
                else
                    RaiseNetworkEvent(new PlayTTSEvent(lexiconSoundData, audioSource, isWhisper: true, distanceOverride: data.AudioRangeOverride), session);
            }
            else
                RaiseNetworkEvent(new PlayTTSEvent(fullSoundData, audioSource, isWhisper: true, distanceOverride: data.AudioRangeOverride), session);

        }
        // DS14-end
    }

    // DS14-start: PVS can follow a movable remote eye, so establish ordinary listeners by their attached entities first.
    private Dictionary<ICommonSession, ChatSystem.ICChatRecipientData> GetExpandedVoiceRecipients(EntityUid source, float voiceRange)
    {
        var recipients = new Dictionary<ICommonSession, ChatSystem.ICChatRecipientData>();
        var sourceXform = Transform(source);
        var sourcePosition = _transform.GetWorldPosition(sourceXform);

        foreach (var session in Filter.Pvs(source).Recipients)
        {
            if (session.AttachedEntity is not { Valid: true } listener ||
                !TryComp(listener, out TransformComponent? listenerXform) ||
                listenerXform.MapID != sourceXform.MapID)
            {
                continue;
            }

            var distance = (sourcePosition - _transform.GetWorldPosition(listenerXform)).Length();
            if (distance >= voiceRange)
                continue;

            recipients.TryAdd(session, new ChatSystem.ICChatRecipientData(distance, false));
        }

        RaiseLocalEvent(new ExpandICChatRecipientsEvent(source, voiceRange, recipients));

        return recipients;
    }
    // DS14-end

    private bool NeedsLexiconTTS(
        ProtoId<LanguagePrototype> languageId,
        IEnumerable<ICommonSession> recipients,
        HashSet<ICommonSession> understanding)
    {
        if (string.IsNullOrEmpty(languageId))
            return false;

        if (!_prototypeManager.TryIndex(languageId, out var languageProto) || !languageProto.GenerateTTSForLexicon)
            return false;

        foreach (var session in recipients)
        {
            if (!understanding.Contains(session))
                return true;
        }

        return false;
    }

    // ReSharper disable once InconsistentNaming
    private async Task<byte[]?> GenerateTTS(string text, string speaker, bool isWhisper = false)
    {
        var textSanitized = Sanitize(text);
        if (textSanitized == "") return null;
        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        var ssmlTraits = SoundTraits.RateFast;
        if (isWhisper)
            ssmlTraits = SoundTraits.PitchVerylow;
        var textSsml = ToSsmlText(textSanitized, ssmlTraits);

        return await _ttsManager.ConvertTextToSpeech(speaker, textSsml);
    }
}
