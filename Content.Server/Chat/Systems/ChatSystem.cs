using System.Globalization;
using System.Linq;
using System.Text;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Speech.EntitySystems;
using Content.Server.Speech.Prototypes;
using Content.Server.Station.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Players;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Radio;
using Content.Shared.Station.Components;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Utility;
using Content.Shared.Corvax.TTS;
using Content.Shared.Dataset;
using Content.DeadSpace.Interfaces.Server;
using Content.Shared.DeadSpace.Languages.Components;
using Content.Shared.DeadSpace.Heartbeat;
using Content.Server.DeadSpace.Languages;
using Content.Server.Audio;

namespace Content.Server.Chat.Systems;

// TODO refactor whatever active warzone this class and chatmanager have become
/// <summary>
///     ChatSystem is responsible for in-simulation chat handling, such as whispering, speaking, emoting, etc.
///     ChatSystem depends on ChatManager to actually send the messages.
/// </summary>
public sealed partial class ChatSystem : SharedChatSystem
{
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IChatSanitizationManager _sanitizer = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    //[Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ReplacementAccentSystem _wordreplacement = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly LanguageSystem _language = default!; // DS14-Languages
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!; // DS14
    private IServerChatFilter? _chatFilter; // DS14-chat-filter

    private bool _loocEnabled = true;
    private bool _deadLoocEnabled;
    private bool _critLoocEnabled;
    private readonly bool _adminLoocEnabled = true;
    private string _centcommTTS = "Widowmaker";
    private static readonly ProtoId<DatasetPrototype> CentcommAnnouncementVoiceDataset = "CentcommAnnouncementVoice";

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configurationManager, CCVars.LoocEnabled, OnLoocEnabledChanged, true);
        Subs.CVar(_configurationManager, CCVars.DeadLoocEnabled, OnDeadLoocEnabledChanged, true);
        Subs.CVar(_configurationManager, CCVars.CritLoocEnabled, OnCritLoocEnabledChanged, true);

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameChange);

        IoCManager.Instance!.TryResolveType(out _chatFilter); // DS14-chat-filter
    }

    private void OnLoocEnabledChanged(bool val)
    {
        if (_loocEnabled == val) return;

        _loocEnabled = val;
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString(val ? "chat-manager-looc-chat-enabled-message" : "chat-manager-looc-chat-disabled-message"));
    }

    private void OnDeadLoocEnabledChanged(bool val)
    {
        if (_deadLoocEnabled == val) return;

        _deadLoocEnabled = val;
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString(val ? "chat-manager-dead-looc-chat-enabled-message" : "chat-manager-dead-looc-chat-disabled-message"));
    }

    private void OnCritLoocEnabledChanged(bool val)
    {
        if (_critLoocEnabled == val)
            return;

        _critLoocEnabled = val;
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString(val ? "chat-manager-crit-looc-chat-enabled-message" : "chat-manager-crit-looc-chat-disabled-message"));
    }

    private void OnGameChange(GameRunLevelChangedEvent ev)
    {
        _centcommTTS = _random.Pick(_prototypeManager.Index(CentcommAnnouncementVoiceDataset).Values);

        switch (ev.New)
        {
            case GameRunLevel.InRound:
                if (!_configurationManager.GetCVar(CCVars.OocEnableDuringRound))
                    _configurationManager.SetCVar(CCVars.OocEnabled, false);
                break;
            case GameRunLevel.PostRound:
            case GameRunLevel.PreRoundLobby:
                if (!_configurationManager.GetCVar(CCVars.OocEnableDuringRound))
                    _configurationManager.SetCVar(CCVars.OocEnabled, true);
                break;
        }
    }

    /// <inheritdoc />
    public override void TrySendInGameICMessage(
        EntityUid source,
        string message,
        InGameICChatType desiredType,
        bool hideChat,
        bool hideLog = false,
        IConsoleShell? shell = null,
        ICommonSession? player = null,
        string? nameOverride = null,
        bool checkRadioPrefix = true,
        bool ignoreActionBlocker = false)
    {
        TrySendInGameICMessage(source, message, desiredType, hideChat ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal, hideLog, shell, player, nameOverride, checkRadioPrefix, ignoreActionBlocker);
    }

    /// <inheritdoc />
    public override void TrySendInGameICMessage(
        EntityUid source,
        string message,
        InGameICChatType desiredType,
        ChatTransmitRange range,
        bool hideLog = false,
        IConsoleShell? shell = null,
        ICommonSession? player = null,
        string? nameOverride = null,
        bool checkRadioPrefix = true,
        bool ignoreActionBlocker = false
        )
    {
        if (HasComp<GhostComponent>(source))
        {
            // Ghosts can only send dead chat messages, so we'll forward it to InGame OOC.
            TrySendInGameOOCMessage(source, message, InGameOOCChatType.Dead, range == ChatTransmitRange.HideChat, shell, player);
            return;
        }

        if (player != null && _chatManager.HandleRateLimit(player) != RateLimitStatus.Allowed)
            return;

        // Sus
        if (player?.AttachedEntity is { Valid: true } entity && source != entity)
        {
            return;
        }

        if (!CanSendInGame(message, shell, player))
            return;

        ignoreActionBlocker = CheckIgnoreSpeechBlocker(source, ignoreActionBlocker);

        // this method is a disaster
        // every second i have to spend working with this code is fucking agony
        // scientists have to wonder how any of this was merged
        // coding any game admin feature that involves chat code is pure torture
        // changing even 10 lines of code feels like waterboarding myself
        // and i dont feel like vibe checking 50 code paths
        // so we set this here
        // todo free me from chat code
        if (player != null)
        {
            _chatManager.EnsurePlayer(player.UserId).AddEntity(GetNetEntity(source));
        }

        if (desiredType == InGameICChatType.Speak && message.StartsWith(LocalPrefix))
        {
            // prevent radios and remove prefix.
            checkRadioPrefix = false;
            message = message[1..];
        }

        bool shouldCapitalize = (desiredType != InGameICChatType.Emote);
        bool shouldPunctuate = _configurationManager.GetCVar(CCVars.ChatPunctuation);
        // Capitalizing the word I only happens in English, so we check language here
        bool shouldCapitalizeTheWordI = (!CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Parent.Name == "en")
            || (CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Name == "en");

        // DS14-chat-filter-start
        if (_chatFilter != null && _chatFilter.NotAllowedMessage(source, message))
            return;
        // DS14-chat-filter-end

        message = SanitizeInGameICMessage(source, message, out var emoteStr, shouldCapitalize, shouldPunctuate, shouldCapitalizeTheWordI);

        // Was there an emote in the message? If so, send it.
        if (player != null && emoteStr != message && emoteStr != null)
        {
            SendEntityEmote(source, emoteStr, range, nameOverride, ignoreActionBlocker);
        }

        // This can happen if the entire string is sanitized out.
        if (string.IsNullOrEmpty(message))
            return;

        // This message may have a radio prefix, and should then be whispered to the resolved radio channel
        if (checkRadioPrefix)
        {
            if (TryProcessRadioMessage(source, message, out var modMessage, out var channel))
            {
                SendEntityWhisper(source, modMessage, range, channel, nameOverride, hideLog, ignoreActionBlocker);
                return;
            }
        }

        // Otherwise, send whatever type.
        switch (desiredType)
        {
            case InGameICChatType.Speak:
                SendEntitySpeak(source, message, range, nameOverride, hideLog, ignoreActionBlocker);
                break;
            case InGameICChatType.Whisper:
                SendEntityWhisper(source, message, range, null, nameOverride, hideLog, ignoreActionBlocker);
                break;
            case InGameICChatType.Emote:
                SendEntityEmote(source, message, range, nameOverride, hideLog: hideLog, ignoreActionBlocker: ignoreActionBlocker);
                break;
        }
    }

    /// <inheritdoc />
    public override void TrySendInGameOOCMessage(
        EntityUid source,
        string message,
        InGameOOCChatType type,
        bool hideChat,
        IConsoleShell? shell = null,
        ICommonSession? player = null
        )
    {
        if (!CanSendInGame(message, shell, player))
            return;

        if (player != null && _chatManager.HandleRateLimit(player) != RateLimitStatus.Allowed)
            return;

        // It doesn't make any sense for a non-player to send in-game OOC messages, whereas non-players may be sending
        // in-game IC messages.
        if (player?.AttachedEntity is not { Valid: true } entity || source != entity)
            return;

        // DS14-chat-filter-start
        if (_chatFilter != null && _chatFilter.NotAllowedMessage(source, message))
            return;
        // DS14-chat-filter-end

        message = SanitizeInGameOOCMessage(message);

        var sendType = type;
        // If dead player LOOC is disabled, unless you are an admin with Moderator perms, send dead messages to dead chat
        if ((_adminManager.IsAdmin(player) && _adminManager.HasAdminFlag(player, AdminFlags.Moderator)) // Override if admin
            || _deadLoocEnabled
            || (!HasComp<GhostComponent>(source) && !_mobStateSystem.IsDead(source))) // Check that player is not dead
        {
        }
        else
            sendType = InGameOOCChatType.Dead;

        // If crit player LOOC is disabled, don't send the message at all.
        if (!_critLoocEnabled && _mobStateSystem.IsCritical(source))
            return;

        // Systems can differentiate Looc and DeadChat by type, and cancel the speak attempt if necessary.
        var ev = new InGameOocMessageAttemptEvent(player, sendType);
        RaiseLocalEvent(source, ref ev, true);
        if (ev.Cancelled)
            return;

        switch (sendType)
        {
            case InGameOOCChatType.Dead:
                SendDeadChat(source, player, message, hideChat);
                break;
            case InGameOOCChatType.Looc:
                SendLOOC(source, player, message, hideChat);
                break;
        }
    }

    #region Announcements

    /// DS14-Languages: extra params prevent override, using new instead
    public void DispatchGlobalAnnouncement(
        string message,
        string? sender = null,
        bool playSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null,
        string originalMessage = "",
        EntityUid? author = null,
        string? voice = null,
        bool usePresetTTS = false,
        string? languageId = null // DS14-Languages
        )
    {
        languageId = string.IsNullOrEmpty(languageId) ? LanguageSystem.DefaultLanguageId : languageId;

        sender ??= Loc.GetString("chat-manager-sender-announcement");

        // DS14-Languages-start
        var filter = Filter.Broadcast();

        string lexiconMessage = _language.TransformWord(message, languageId);

        string langName = _language.GetLangName(languageId);

        var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message-lang", ("sender", sender), ("language", langName), ("message", FormattedMessage.EscapeText(message)));

        // DS14-chat-filter-start
        if (_chatFilter != null && _chatFilter.NotAllowedMessage(wrappedMessage))
            return;
        // DS14-chat-filter-end

        var understanding = _language.GetUnderstanding(languageId);

        var lexiconWrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender), ("message", FormattedMessage.EscapeText(lexiconMessage)));

        foreach (var session in filter.Recipients)
        {
            if (!understanding.Contains(session))
                _chatManager.ChatMessageToOne(ChatChannel.Radio, lexiconMessage, lexiconWrappedMessage, default, false, session.Channel, colorOverride, true);
            else
                _chatManager.ChatMessageToOne(ChatChannel.Radio, message, wrappedMessage, default, false, session.Channel, colorOverride, true);
        }
        // DS14-Languages-end

        // Fucked up logic ahead... FIX THIS PLEASE.

        if (playSound)
        {
            if (announcementSound == null)
            {
                if (sender == Loc.GetString("chat-manager-sender-announcement")) announcementSound = CentComAnnouncementSound; // Corvax-Announcements: Support custom alert sound from admin panel
            }

            _sound.PlayAnnonceGlobal(Filter.Broadcast(), announcementSound ?? DefaultAnnouncementSound, announcementSound?.Params ?? AudioParams.Default.WithVolume(-2f), true); //DS14

            if (author != null && TryComp<TTSComponent>(author.Value, out var tts) && tts.VoicePrototypeId != null) // For comms console announcements
            {
                var ev = new AnnounceSpokeEvent(tts.VoicePrototypeId, originalMessage, lexiconMessage, languageId, filter, author.Value);
                RaiseLocalEvent(ev);
            }
            else if (usePresetTTS && sender == Loc.GetString("chat-manager-sender-announcement")) // For admin announcements from Centcomm with preset voices
            {
                voice = _centcommTTS;
                var ev = new AnnounceSpokeEvent(voice, originalMessage, lexiconMessage, languageId, filter, null);
                RaiseLocalEvent(ev);
            }
            else if (voice != null) // For admin announcements
            {
                var ev = new AnnounceSpokeEvent(voice, originalMessage, lexiconMessage, languageId, filter, null);
                RaiseLocalEvent(ev);
            }
        }
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Global station announcement from {sender}: {message}");
    }

    // DS14-announce-start
    public void DispatchAdminFilteredAnnouncement(
        Filter filter,
        string message,
        string? sender = null,
        bool playSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null,
        string originalMessage = "",
        string? voice = null,
        bool usePresetTTS = false,
        string? languageId = null)
    {
        languageId = string.IsNullOrEmpty(languageId) ? LanguageSystem.DefaultLanguageId : languageId;

        sender ??= Loc.GetString("chat-manager-sender-announcement");

        var lexiconMessage = _language.TransformWord(message, languageId);
        var langName = _language.GetLangName(languageId);
        var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message-lang",
            ("sender", sender),
            ("language", langName),
            ("message", FormattedMessage.EscapeText(message)));

        if (_chatFilter != null && _chatFilter.NotAllowedMessage(wrappedMessage))
            return;

        var understanding = _language.GetUnderstanding(languageId);
        var lexiconWrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message",
            ("sender", sender),
            ("message", FormattedMessage.EscapeText(lexiconMessage)));

        foreach (var session in filter.Recipients)
        {
            if (!understanding.Contains(session))
                _chatManager.ChatMessageToOne(ChatChannel.Radio, lexiconMessage, lexiconWrappedMessage, default, false, session.Channel, colorOverride, true);
            else
                _chatManager.ChatMessageToOne(ChatChannel.Radio, message, wrappedMessage, default, false, session.Channel, colorOverride, true);
        }

        if (playSound)
        {
            if (announcementSound == null)
            {
                if (sender == Loc.GetString("chat-manager-sender-announcement"))
                    announcementSound = CentComAnnouncementSound;
            }

            _sound.PlayAnnonceGlobal(filter, announcementSound ?? DefaultAnnouncementSound, announcementSound?.Params ?? AudioParams.Default.WithVolume(-2f), true);//DS14

            if (usePresetTTS && sender == Loc.GetString("chat-manager-sender-announcement"))
            {
                voice = _centcommTTS;
                var ev = new AnnounceSpokeEvent(voice, originalMessage, lexiconMessage, languageId, filter, null);
                RaiseLocalEvent(ev);
            }
            else if (voice != null)
            {
                var ev = new AnnounceSpokeEvent(voice, originalMessage, lexiconMessage, languageId, filter, null);
                RaiseLocalEvent(ev);
            }
        }

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Filtered station announcement from {sender}: {message}");
    }
    // DS14-announce-end

    /// <inheritdoc />
    public override void DispatchFilteredAnnouncement(
        Filter filter,
        string message,
        EntityUid? source = null,
        string? sender = null,
        bool playSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null)
    {
        sender ??= Loc.GetString("chat-manager-sender-announcement");

        var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender), ("message", FormattedMessage.EscapeText(message)));
        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, wrappedMessage, source ?? default, false, true, colorOverride);
        if (playSound)
        {
            _sound.PlayAnnonceGlobal(filter, announcementSound ?? DefaultAnnouncementSound, AudioParams.Default.WithVolume(-2f), true);//DS14
        }
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Station Announcement from {sender}: {message}");
    }

    /// DS14-Languages: extra params prevent override, using new instead
    public void DispatchStationAnnouncement(
        EntityUid source,
        string message,
        string? sender = null,
        bool playDefaultSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null,
        string? voice = null,
        // DS14-start
        string? languageId = null,
        Filter? recipientFilter = null,
        bool usePresetTTS = false)
        // DS14-end
    {
        languageId = string.IsNullOrEmpty(languageId) ? LanguageSystem.DefaultLanguageId : languageId;

        sender ??= Loc.GetString("chat-manager-sender-announcement");

        string lexiconMessage = _language.TransformWord(message, languageId);

        string langName = _language.GetLangName(languageId);

        var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message-lang", ("sender", sender), ("language", langName), ("message", FormattedMessage.EscapeText(message)));
        var lexiconWrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender), ("message", FormattedMessage.EscapeText(lexiconMessage)));

        var station = _stationSystem.GetOwningStation(source);

        if (station == null)
        {
            // you can't make a station announcement without a station
            return;
        }

        if (!TryComp<StationDataComponent>(station, out var stationDataComp)) return;

        var filterStation = recipientFilter ?? _stationSystem.GetInStation(stationDataComp); // DS14
        var filterUnderstanding = Filter.Empty();
        var filterNotUnderstanding = Filter.Empty();

        foreach (var session in filterStation.Recipients)
        {
            if (session.AttachedEntity is not { } entity)
                continue;

            if (_language.KnowsLanguage(entity, languageId))
                filterUnderstanding.AddPlayer(session);
            else
                filterNotUnderstanding.AddPlayer(session);
        }

        _chatManager.ChatMessageToManyFiltered(filterUnderstanding, ChatChannel.Radio, message, wrappedMessage, source, false, true, colorOverride);
        _chatManager.ChatMessageToManyFiltered(filterNotUnderstanding, ChatChannel.Radio, lexiconMessage, lexiconWrappedMessage, source, false, true, colorOverride);

        // плохая реализация, лучше переписать AnnounceSpoke
        // DS14-start
        if (usePresetTTS)
        {
            var ev = new AnnounceSpokeEvent(_centcommTTS, message, lexiconMessage, languageId, filterStation, null);
            RaiseLocalEvent(ev);
        }
        else if (!string.IsNullOrEmpty(voice))
        {
            var ev = new AnnounceSpokeEvent(voice, message, lexiconMessage, languageId, filterStation, null);
            RaiseLocalEvent(ev);
        }
        // DS14-end

        if (playDefaultSound)
        {
            _sound.PlayAnnonceGlobal(filterStation, announcementSound ?? DefaultAnnouncementSound, AudioParams.Default.WithVolume(-2f), true); //DS14
        }

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Station Announcement on {station} from {sender}: {message}");
    }

    #endregion

    #region Private API
    private void SendEntitySpeak(
        EntityUid source,
        string originalMessage,
        ChatTransmitRange range,
        string? nameOverride,
        bool hideLog = false,
        bool ignoreActionBlocker = false
        )
    {
        if (!_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
            return;
        // DS14-start
        if (_mobStateSystem.IsPreCritical(source))
        {
            SendEntityWhisper(source, originalMessage, range, null, nameOverride, hideLog, ignoreActionBlocker);
            return;
        }
        // DS14-end
        var message = TransformSpeech(source, originalMessage);

        if (message.Length == 0)
            return;

        var speech = GetSpeechVerb(source, message);

        // get the entity's apparent name (if no override provided).
        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            var nameEv = new TransformSpeakerNameEvent(source, Name(source));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.VoiceName;
            // Check for a speech verb override
            if (nameEv.SpeechVerb != null && _prototypeManager.Resolve(nameEv.SpeechVerb, out var proto))
                speech = proto;
        }

        name = FormattedMessage.EscapeText(name);

        // DS14-Languages-start
        string verb = Loc.GetString(_random.Pick(speech.SpeechVerbStrings));
        string lexiconMessage = message;

        if (TryComp<LanguageComponent>(source, out var language))
            lexiconMessage = _language.TransformWord(message, language.SelectedLanguage);

        string langName = _language.GetLangName(source, language);

        var wrappedMessage = Loc.GetString(speech.Bold ? "chat-manager-entity-say-bold-wrap-message-lang" : "chat-manager-entity-say-wrap-message-lang",
            ("entityName", name),
            ("verb", verb),
            ("language", langName),
            ("fontType", speech.FontId),
            ("fontSize", speech.FontSize),
            ("message", FormattedMessage.EscapeText(message)));

        var wrappedMessageUnk = Loc.GetString(speech.Bold ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message",
            ("entityName", name),
            ("verb", verb),
            ("fontType", speech.FontId),
            ("fontSize", speech.FontSize),
            ("message", FormattedMessage.EscapeText(message)));

        string lexiconWrappedMessage = wrappedMessage;

        if (language != null)
        {
            lexiconMessage = _language.TransformWord(message, language.SelectedLanguage);

            lexiconWrappedMessage = wrappedMessageUnk.Replace(FormattedMessage.EscapeText(message), FormattedMessage.EscapeText(lexiconMessage));
        }

        var selectedLanguage = language != null ? language.SelectedLanguage : string.Empty;

        SendInVoiceRange(ChatChannel.Local, message, wrappedMessage, source, range, null, lexiconMessage, lexiconWrappedMessage, selectedLanguage);

        var ev = new EntitySpokeEvent(source, message, originalMessage, lexiconMessage, selectedLanguage, null, null);
        RaiseLocalEvent(source, ev, true);
        // DS14-Languages-end

        // To avoid logging any messages sent by entities that are not players, like vendors, cloning, etc.
        // Also doesn't log if hideLog is true.
        if (!HasComp<ActorComponent>(source) || hideLog)
            return;

        if (originalMessage == message)
        {
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {source} as {name}: {originalMessage}.");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {source}: {originalMessage}.");
        }
        else
        {
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Say from {source} as {name}, original: {originalMessage}, transformed: {message}.");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Say from {source}, original: {originalMessage}, transformed: {message}.");
        }
    }

    private void SendEntityWhisper(
        EntityUid source,
        string originalMessage,
        ChatTransmitRange range,
        RadioChannelPrototype? channel,
        string? nameOverride,
        bool hideLog = false,
        bool ignoreActionBlocker = false
        )
    {
        if (!_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
            return;

        var message = TransformSpeech(source, FormattedMessage.RemoveMarkupOrThrow(originalMessage));
        if (message.Length == 0)
            return;

        var obfuscatedMessage = ObfuscateMessageReadability(message, 0.2f);

        // get the entity's name by visual identity (if no override provided).
        string nameIdentity = FormattedMessage.EscapeText(nameOverride ?? Identity.Name(source, EntityManager));
        // get the entity's name by voice (if no override provided).
        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            var nameEv = new TransformSpeakerNameEvent(source, Name(source));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.VoiceName;
        }
        name = FormattedMessage.EscapeText(name);

        // DS14-Languages-start
        string lexiconMessage = message;

        if (TryComp<LanguageComponent>(source, out var language))
            lexiconMessage = _language.TransformWord(message, language.SelectedLanguage);

        string langName = _language.GetLangName(source, language);

        var wrappedMessage = Loc.GetString("chat-manager-entity-whisper-wrap-message-lang",
            ("entityName", name), ("language", langName), ("message", FormattedMessage.EscapeText(message)));

        var wrappedobfuscatedMessage = Loc.GetString("chat-manager-entity-whisper-wrap-message-lang",
            ("entityName", nameIdentity), ("language", langName), ("message", FormattedMessage.EscapeText(obfuscatedMessage)));

        var wrappedMessageUnk = Loc.GetString("chat-manager-entity-whisper-wrap-message",
            ("entityName", name), ("message", FormattedMessage.EscapeText(message)));

        var wrappedobfuscatedMessageUnk = Loc.GetString("chat-manager-entity-whisper-wrap-message",
            ("entityName", nameIdentity), ("message", FormattedMessage.EscapeText(obfuscatedMessage)));

        var wrappedUnknownMessage = Loc.GetString("chat-manager-entity-whisper-unknown-wrap-message",
            ("message", FormattedMessage.EscapeText(obfuscatedMessage)));


        var newObfuscatedMessage = ObfuscateMessageReadability(lexiconMessage, 0.2f);

        var newWrappedMessage = wrappedMessageUnk.Replace(FormattedMessage.EscapeText(message), FormattedMessage.EscapeText(lexiconMessage));
        var newWrappedobfuscatedMessage = wrappedobfuscatedMessageUnk.Replace(FormattedMessage.EscapeText(obfuscatedMessage), FormattedMessage.EscapeText(newObfuscatedMessage));
        var newWrappedUnknownMessage = wrappedUnknownMessage.Replace(FormattedMessage.EscapeText(obfuscatedMessage), FormattedMessage.EscapeText(newObfuscatedMessage));

        // DS14-Languages-end

        foreach (var (session, data) in GetRecipients(source, WhisperMuffledRange))
        {
            EntityUid listener;

            if (session.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            listener = session.AttachedEntity.Value;

            if (MessageRangeCheck(session, data, range) != MessageRangeCheckResult.Full)
                continue; // Won't get logged to chat, and ghosts are too far away to see the pop-up, so we just won't send it to them.

            string recipientMessage;
            string recipientWrappedMessage;

            // DS14-Languages-start
            if (language != null && !_language.KnowsLanguage(listener, language.SelectedLanguage))
            {
                if (data.Range <= WhisperClearRange || data.Observer)
                    (recipientMessage, recipientWrappedMessage) = (lexiconMessage, newWrappedMessage);
                else if (_examineSystem.InRangeUnOccluded(source, listener, WhisperMuffledRange))
                    (recipientMessage, recipientWrappedMessage) = (newObfuscatedMessage, newWrappedobfuscatedMessage);
                else
                    (recipientMessage, recipientWrappedMessage) = (newObfuscatedMessage, newWrappedUnknownMessage);
            }
            // DS14-Languages-end
            else if (data.Range <= WhisperClearRange || data.Observer)
            {
                (recipientMessage, recipientWrappedMessage) = (message, wrappedMessage);
            }
            //If listener is too far, they only hear fragments of the message
            else if (_examineSystem.InRangeUnOccluded(source, listener, WhisperMuffledRange))
            {
                (recipientMessage, recipientWrappedMessage) = (obfuscatedMessage, wrappedobfuscatedMessage);
            }
            //If listener is too far and has no line of sight, they can't identify the whisperer's identity
            else
            {
                (recipientMessage, recipientWrappedMessage) = (obfuscatedMessage, wrappedUnknownMessage);
            }

            if (IsCriticalHearingBlocked(listener, ChatChannel.Whisper))
            {
                var hearingMessage = GetCriticalHearingMessage(listener, source);
                _chatManager.ChatMessageToOne(
                    ChatChannel.Whisper,
                    hearingMessage,
                    hearingMessage,
                    default,
                    false,
                    session.Channel);
                continue;
            }

            _chatManager.ChatMessageToOne(
                ChatChannel.Whisper,
                recipientMessage,
                recipientWrappedMessage,
                source,
                false,
                session.Channel);
        }

        _replay.RecordServerMessage(new ChatMessage(ChatChannel.Whisper, message, wrappedMessage, GetNetEntity(source), null, MessageRangeHideChatForReplay(range)));

        var selectedLanguage = language != null ? language.SelectedLanguage : string.Empty; // DS14-Languages
        var ev = new EntitySpokeEvent(source, message, originalMessage, lexiconMessage, selectedLanguage, channel, obfuscatedMessage); // DS14-Languages

        RaiseLocalEvent(source, ev, true);
        if (!hideLog)
            if (originalMessage == message)
            {
                if (name != Name(source))
                    _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Whisper from {source} as {name}: {originalMessage}.");
                else
                    _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Whisper from {source}: {originalMessage}.");
            }
            else
            {
                if (name != Name(source))
                    _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Whisper from {source} as {name}, original: {originalMessage}, transformed: {message}.");
                else
                    _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Whisper from {source}, original: {originalMessage}, transformed: {message}.");
            }
    }

    protected override void SendEntityEmote(
        EntityUid source,
        string action,
        ChatTransmitRange range,
        string? nameOverride,
        bool hideLog = false,
        bool checkEmote = true,
        bool ignoreActionBlocker = false,
        NetUserId? author = null
        )
    {
        if (!_actionBlocker.CanEmote(source) && !ignoreActionBlocker)
            return;

        // get the entity's apparent name (if no override provided).
        var ent = Identity.Entity(source, EntityManager);
        string name = FormattedMessage.EscapeText(nameOverride ?? Name(ent));

        // Emotes use Identity.Name, since it doesn't actually involve your voice at all.
        var wrappedMessage = Loc.GetString("chat-manager-entity-me-wrap-message",
            ("entityName", name),
            ("entity", ent),
            ("message", FormattedMessage.RemoveMarkupOrThrow(action)));

        if (checkEmote &&
            !TryEmoteChatInput(source, action))
            return;

        SendInVoiceRange(ChatChannel.Emotes, action, wrappedMessage, source, range, author);
        if (!hideLog)
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Emote from {source} as {name}: {action}");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Emote from {source}: {action}");
    }

    // ReSharper disable once InconsistentNaming
    private void SendLOOC(EntityUid source, ICommonSession player, string message, bool hideChat)
    {
        var name = FormattedMessage.EscapeText(Identity.Name(source, EntityManager));

        if (_adminManager.IsAdmin(player))
        {
            if (!_adminLoocEnabled) return;
        }
        else if (!_loocEnabled) return;

        // If crit player LOOC is disabled, don't send the message at all.
        if (!_critLoocEnabled && _mobStateSystem.IsCritical(source))
            return;

        var wrappedMessage = Loc.GetString("chat-manager-entity-looc-wrap-message",
            ("entityName", name),
            ("message", FormattedMessage.EscapeText(message)));

        SendInVoiceRange(ChatChannel.LOOC, message, wrappedMessage, source, hideChat ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal, player.UserId);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"LOOC from {source}: {message}");
    }

    private void SendDeadChat(EntityUid source, ICommonSession player, string message, bool hideChat)
    {
        var clients = GetDeadChatClients();
        var playerName = FormattedMessage.EscapeText(Name(source)); // DS14
        string wrappedMessage;
        if (_adminManager.IsAdmin(player))
        {
            wrappedMessage = Loc.GetString("chat-manager-send-admin-dead-chat-wrap-message",
                ("adminChannelName", Loc.GetString("chat-manager-admin-channel-name")),
                ("userName", FormattedMessage.EscapeText(player.Channel.UserName)), // DS14
                ("message", FormattedMessage.EscapeText(message)));
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Admin dead chat from {source}: {message}");
        }
        else
        {
            wrappedMessage = Loc.GetString("chat-manager-send-dead-chat-wrap-message",
                ("deadChannelName", Loc.GetString("chat-manager-dead-channel-name")),
                ("playerName", playerName), // DS14
                ("message", FormattedMessage.EscapeText(message)));
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Dead chat from {source}: {message}");
        }

        _chatManager.ChatMessageToMany(ChatChannel.Dead, message, wrappedMessage, source, hideChat, true, clients.ToList(), author: player.UserId);
    }
    #endregion

    #region Utility

    private enum MessageRangeCheckResult
    {
        Disallowed,
        HideChat,
        Full
    }

    /// <summary>
    ///     If hideChat should be set as far as replays are concerned.
    /// </summary>
    private bool MessageRangeHideChatForReplay(ChatTransmitRange range)
    {
        return range == ChatTransmitRange.HideChat;
    }

    /// <summary>
    ///     Checks if a target as returned from GetRecipients should receive the message.
    ///     Keep in mind data.Range is -1 for out of range observers.
    /// </summary>
    private MessageRangeCheckResult MessageRangeCheck(ICommonSession session, ICChatRecipientData data, ChatTransmitRange range)
    {
        var initialResult = MessageRangeCheckResult.Full;
        switch (range)
        {
            case ChatTransmitRange.Normal:
                initialResult = MessageRangeCheckResult.Full;
                break;
            case ChatTransmitRange.GhostRangeLimit:
                initialResult = (data.Observer && data.Range < 0 && !_adminManager.IsAdmin(session)) ? MessageRangeCheckResult.HideChat : MessageRangeCheckResult.Full;
                break;
            case ChatTransmitRange.HideChat:
                initialResult = MessageRangeCheckResult.HideChat;
                break;
            case ChatTransmitRange.NoGhosts:
                initialResult = (data.Observer && !_adminManager.IsAdmin(session)) ? MessageRangeCheckResult.Disallowed : MessageRangeCheckResult.Full;
                break;
        }
        var insistHideChat = data.HideChatOverride ?? false;
        var insistNoHideChat = !(data.HideChatOverride ?? true);
        if (insistHideChat && initialResult == MessageRangeCheckResult.Full)
            return MessageRangeCheckResult.HideChat;
        if (insistNoHideChat && initialResult == MessageRangeCheckResult.HideChat)
            return MessageRangeCheckResult.Full;
        return initialResult;
    }

    /// <summary>
    ///     Sends a chat message to the given players in range of the source entity.
    /// </summary>
    private void SendInVoiceRange(ChatChannel channel, string message, string wrappedMessage, EntityUid source, ChatTransmitRange range, NetUserId? author = null, string? lexiconMessage = null, string? lexiconWrappedMessage = null, string? selectedLanguage = null)
    {
        var totalWrappedMessage = wrappedMessage;
        var totalMessage = message;

        foreach (var (session, data) in GetRecipients(source, VoiceRange))
        {
            // DS14-Languages-start
            EntityUid listener;

            if (session.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            listener = session.AttachedEntity.Value;

            if (lexiconMessage != null && lexiconWrappedMessage != null && selectedLanguage != null && !_language.KnowsLanguage(listener, selectedLanguage))
            {
                totalWrappedMessage = lexiconWrappedMessage;
                totalMessage = lexiconMessage;
            }
            else
            {
                totalWrappedMessage = wrappedMessage;
                totalMessage = message;
            }

            // DS14-Languages-end

            var entRange = MessageRangeCheck(session, data, range);
            if (entRange == MessageRangeCheckResult.Disallowed)
                continue;
            var entHideChat = entRange == MessageRangeCheckResult.HideChat;
            if (IsCriticalHearingBlocked(listener, channel))
            {
                var hearingMessage = GetCriticalHearingMessage(listener, source);
                _chatManager.ChatMessageToOne(
                    channel,
                    hearingMessage,
                    hearingMessage,
                    default,
                    entHideChat,
                    session.Channel,
                    author: author);
                continue;
            }

            _chatManager.ChatMessageToOne(channel, totalMessage, totalWrappedMessage, source, entHideChat, session.Channel, author: author);
        }

        _replay.RecordServerMessage(new ChatMessage(channel, message, wrappedMessage, GetNetEntity(source), null, MessageRangeHideChatForReplay(range)));
    }

    private bool IsCriticalHearingBlocked(EntityUid listener, ChatChannel channel)
    {
        return channel is ChatChannel.Local or ChatChannel.Whisper &&
               HasComp<CritHeartbeatComponent>(listener) &&
               (_mobStateSystem.IsPreCritical(listener) ||
                _mobStateSystem.IsCritical(listener));
    }

    private string GetCriticalHearingMessage(EntityUid listener, EntityUid source)
    {
        return Loc.GetString(listener == source
            ? "dead-space-critical-hearing-self"
            : "dead-space-critical-hearing-others");
    }

    /// <summary>
    ///     Returns true if the given player is 'allowed' to send the given message, false otherwise.
    /// </summary>
    private bool CanSendInGame(string message, IConsoleShell? shell = null, ICommonSession? player = null)
    {
        // Non-players don't have to worry about these restrictions.
        if (player == null)
            return true;

        var mindContainerComponent = player.ContentData()?.Mind;

        if (mindContainerComponent == null)
        {
            shell?.WriteError("You don't have a mind!");
            return false;
        }

        if (player.AttachedEntity is not { Valid: true } _)
        {
            shell?.WriteError("You don't have an entity!");
            return false;
        }

        return !_chatManager.MessageCharacterLimit(player, message);
    }

    // ReSharper disable once InconsistentNaming
    private string SanitizeInGameICMessage(EntityUid source, string message, out string? emoteStr, bool capitalize = true, bool punctuate = false, bool capitalizeTheWordI = true)
    {
        var newMessage = SanitizeMessageReplaceWords(message.Trim());

        GetRadioKeycodePrefix(source, newMessage, out newMessage, out var prefix);

        // Sanitize it first as it might change the word order
        _sanitizer.TrySanitizeEmoteShorthands(newMessage, source, out newMessage, out emoteStr);

        // DS14-chat-filter-start
        if (_chatFilter != null)
            newMessage = _chatFilter.ReplaceWords(newMessage);
        // DS14-chat-filter-end

        if (capitalize)
            newMessage = SanitizeMessageCapital(newMessage);
        if (capitalizeTheWordI)
            newMessage = SanitizeMessageCapitalizeTheWordI(newMessage, "i");
        if (punctuate)
            newMessage = SanitizeMessagePeriod(newMessage);

        return prefix + newMessage;
    }

    private string SanitizeInGameOOCMessage(string message)
    {
        var newMessage = message.Trim();
        newMessage = FormattedMessage.EscapeText(newMessage);

        return newMessage;
    }

    public string TransformSpeech(EntityUid sender, string message)
    {
        var ev = new TransformSpeechEvent(sender, message);
        RaiseLocalEvent(sender, ev, true);

        return ev.Message;
    }

    public bool CheckIgnoreSpeechBlocker(EntityUid sender, bool ignoreBlocker)
    {
        if (ignoreBlocker)
            return ignoreBlocker;

        var ev = new CheckIgnoreSpeechBlockerEvent(sender, ignoreBlocker);
        RaiseLocalEvent(sender, ev, true);

        return ev.IgnoreBlocker;
    }

    private IEnumerable<INetChannel> GetDeadChatClients()
    {
        return Filter.Empty()
            .AddWhereAttachedEntity(HasComp<GhostComponent>)
            .Recipients
            .Union(_adminManager.ActiveAdmins)
            .Select(p => p.Channel);
    }

    private string SanitizeMessagePeriod(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;
        // Adds a period if the last character is a letter.
        if (char.IsLetter(message[^1]))
            message += ".";
        return message;
    }

    public static readonly ProtoId<ReplacementAccentPrototype> ChatSanitize_Accent = "chatsanitize";

    public string SanitizeMessageReplaceWords(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;

        var msg = message;

        msg = _wordreplacement.ApplyReplacements(msg, ChatSanitize_Accent);

        return msg;
    }

    /// <summary>
    ///     Returns list of players and ranges for all players withing some range. Also returns observers with a range of -1.
    /// </summary>
    private Dictionary<ICommonSession, ICChatRecipientData> GetRecipients(EntityUid source, float voiceGetRange)
    {
        // TODO proper speech occlusion

        var recipients = new Dictionary<ICommonSession, ICChatRecipientData>();
        var ghostHearing = GetEntityQuery<GhostHearingComponent>();
        var xforms = GetEntityQuery<TransformComponent>();

        var transformSource = xforms.GetComponent(source);
        var sourceMapId = transformSource.MapID;
        var sourceCoords = transformSource.Coordinates;

        foreach (var player in _playerManager.Sessions)
        {
            if (player.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            var transformEntity = xforms.GetComponent(playerEntity);

            if (transformEntity.MapID != sourceMapId)
                continue;

            var observer = ghostHearing.HasComponent(playerEntity);

            // even if they are a ghost hearer, in some situations we still need the range
            if (sourceCoords.TryDistance(EntityManager, transformEntity.Coordinates, out var distance) && distance < voiceGetRange)
            {
                recipients.Add(player, new ICChatRecipientData(distance, observer));
                continue;
            }

            if (observer)
                recipients.Add(player, new ICChatRecipientData(-1, true));
        }

        RaiseLocalEvent(new ExpandICChatRecipientsEvent(source, voiceGetRange, recipients));
        return recipients;
    }

    public readonly record struct ICChatRecipientData(float Range, bool Observer, bool? HideChatOverride = null)
    {
    }

    private string ObfuscateMessageReadability(string message, float chance)
    {
        var modifiedMessage = new StringBuilder(message);

        for (var i = 0; i < message.Length; i++)
        {
            if (char.IsWhiteSpace((modifiedMessage[i])))
            {
                continue;
            }

            if (_random.Prob(1 - chance))
            {
                modifiedMessage[i] = '~';
            }
        }

        return modifiedMessage.ToString();
    }

    public string BuildGibberishString(IReadOnlyList<char> charOptions, int length)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            sb.Append(_random.Pick(charOptions));
        }
        return sb.ToString();
    }

    #endregion
}

/// <summary>
///     This event is raised before chat messages are sent out to clients. This enables some systems to send the chat
///     messages to otherwise out-of view entities (e.g. for multiple viewports from cameras).
/// </summary>
public record ExpandICChatRecipientsEvent(EntityUid Source, float VoiceRange, Dictionary<ICommonSession, ChatSystem.ICChatRecipientData> Recipients)
{
}

// DS14-Languages: Event types moved to Content.Shared/Chat/SharedChatEvents.cs
// Enums (InGameICChatType, InGameOOCChatType, ChatTransmitRange) are in Content.Shared/Chat/SharedChatSystem.cs
