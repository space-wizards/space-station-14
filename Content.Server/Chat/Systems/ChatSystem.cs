using System.Globalization;
using System.Linq;
using System.Text;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Players.RateLimiting;
using Content.Server.Speech.Components;
using Content.Server.Speech.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Players;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Radio;
using Content.Shared.Whitelist;
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
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ReplacementAccentSystem _wordreplacement = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;

    public const int VoiceRange = 10; // how far voice goes in world units
    public const int WhisperClearRange = 2; // how far whisper goes while still being understandable, in world units
    public const int WhisperMuffledRange = 5; // how far whisper goes at all, in world units
    public const string DefaultAnnouncementSound = "/Audio/Announcements/announce.ogg";

    private bool _loocEnabled = true;
    private bool _deadLoocEnabled;
    private bool _critLoocEnabled;
    private readonly bool _adminLoocEnabled = true;

    private static ProtoId<CommunicationChannelPrototype> _channelDefaultAdminChannel = "AdminChat";
    private static ProtoId<CommunicationChannelPrototype> _channelDefaultAdminAlertChannel = "AdminAlert";
    private static ProtoId<CommunicationChannelPrototype> _channelDefaultServerAnnouncementChannel = "ServerAnnouncement";
    private static ProtoId<CommunicationChannelPrototype> _channelDefaultNotificationChannel = "GameMessage";

    public override void Initialize()
    {
        base.Initialize();
        CacheEmotes();
        Subs.CVar(_configurationManager, CCVars.LoocEnabled, OnLoocEnabledChanged, true);
        Subs.CVar(_configurationManager, CCVars.DeadLoocEnabled, OnDeadLoocEnabledChanged, true);
        Subs.CVar(_configurationManager, CCVars.CritLoocEnabled, OnCritLoocEnabledChanged, true);

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameChange);
    }

    #region Cvar Subscription Notifications

    // CHAT-TODO: See how you can implement this
    private void OnLoocEnabledChanged(bool val)
    {
        if (_loocEnabled == val) return;

        _loocEnabled = val;
        //_chatManager.DispatchServerAnnouncement(
        //    Loc.GetString(val ? "chat-manager-looc-chat-enabled-message" : "chat-manager-looc-chat-disabled-message"));
    }

    // CHAT-TODO: See how you can implement this
    private void OnDeadLoocEnabledChanged(bool val)
    {
        if (_deadLoocEnabled == val) return;

        _deadLoocEnabled = val;
        //_chatManager.DispatchServerAnnouncement(
        //    Loc.GetString(val ? "chat-manager-dead-looc-chat-enabled-message" : "chat-manager-dead-looc-chat-disabled-message"));
    }

    // CHAT-TODO: See how you can implement this
    private void OnCritLoocEnabledChanged(bool val)
    {
        if (_critLoocEnabled == val)
            return;

        _critLoocEnabled = val;
        //_chatManager.DispatchServerAnnouncement(
        //    Loc.GetString(val ? "chat-manager-crit-looc-chat-enabled-message" : "chat-manager-crit-looc-chat-disabled-message"));
    }

    private void OnGameChange(GameRunLevelChangedEvent ev)
    {
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

    #endregion

    /// <summary>
    ///     Sends an in-character chat message to relevant clients.
    /// </summary>
    /// <param name="source">The entity that is speaking</param>
    /// <param name="message">The message being spoken or emoted</param>
    /// <param name="desiredType">The chat type</param>
    /// <param name="range">Conceptual range of transmission, if it shows in the chat window, if it shows to far-away ghosts or ghosts at all...</param>
    /// <param name="shell"></param>
    /// <param name="player">The player doing the speaking</param>
    /// <param name="nameOverride">The name to use for the speaking entity. Usually this should just be modified via <see cref="TransformSpeakerNameEvent"/>. If this is set, the event will not get raised.</param>
    /// <param name="ignoreActionBlocker">If set to true, action blocker will not be considered for whether an entity can send this message.</param>
    public void TrySendInGameICMessage(
        EntityUid source,
        string message,
        ICommonSession? player = null
        )
    {
        _chatManager.SendChannelMessage(message, "ICSpeech", player, source);

        // CHAT-TODO: Handled by backup
        /*if (HasComp<GhostComponent>(source))
        {
            // Ghosts can only send dead chat messages, so we'll forward it to InGame OOC.
            TrySendInGameOOCMessage(source, message, InGameOOCChatType.Dead, range == ChatTransmitRange.HideChat, shell, player);
            return;
        }*/

        // CHAT-TODO: Should be handled by the manager
        //if (player != null && _chatManager.HandleRateLimit(player) != RateLimitStatus.Allowed)
        //    return;

        // Handled by entity check?
        /*
        // Sus
        if (player?.AttachedEntity is { Valid: true } entity && source != entity)
        {
            return;
        }*/

        /* CHAT-TODO: Handled by conditions!
        if (!CanSendInGame(message, shell, player))
            return;
        */

        //ignoreActionBlocker = CheckIgnoreSpeechBlocker(source, ignoreActionBlocker);

        // CHAT-TODO: This logging should probably be done in the manager directly!
        /*if (player != null)
        {
            _chatManager.EnsurePlayer(player.UserId).AddEntity(GetNetEntity(source));
        }*/

        // CHAT-TODO: This is set by the channel prototype
        /*if (desiredType == InGameICChatType.Speak && message.StartsWith(LocalPrefix))
        {
            // prevent radios and remove prefix.
            checkRadioPrefix = false;
            message = message[1..];
        }*/

        // CHAT-TODO: Markup!
        /*
        bool shouldCapitalize = (desiredType != InGameICChatType.Emote);
        bool shouldPunctuate = _configurationManager.GetCVar(CCVars.ChatPunctuation);
        // Capitalizing the word I only happens in English, so we check language here
        bool shouldCapitalizeTheWordI = (!CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Parent.Name == "en")
            || (CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Name == "en");

        message = SanitizeInGameICMessage(source, message, out var emoteStr, shouldCapitalize, shouldPunctuate, shouldCapitalizeTheWordI);

        // Was there an emote in the message? If so, send it.
        if (player != null && emoteStr != message && emoteStr != null)
        {
            SendEntityEmote(source, emoteStr, range, nameOverride, ignoreActionBlocker);
        }

        // CHAT-TODO: This needs to have a check in the manager probably
        // This can happen if the entire string is sanitized out.
        if (string.IsNullOrEmpty(message))
            return;
        */

        // CHAT-TODO: Handled by child channels
        // This message may have a radio prefix, and should then be whispered to the resolved radio channel
        /*if (checkRadioPrefix)
        {
            if (TryProccessRadioMessage(source, message, out var modMessage, out var channel))
            {
                SendEntityWhisper(source, modMessage, range, channel, nameOverride, hideLog, ignoreActionBlocker);
                return;
            }
        }*/

        // CHAT-TODO: Handled by channel prototype
        // Otherwise, send whatever type.
        /*
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
        }*/
    }

    public void TrySendInGameOOCMessage(
        EntityUid source,
        string message,
        ICommonSession? player = null
        )
    {
        _chatManager.SendChannelMessage(message, "LOOC", player, source);

        /* CHAT-TODO: Will be handled by conditions
        if (!CanSendInGame(message, shell, player))
            return;

        if (player != null && _chatManager.HandleRateLimit(player) != RateLimitStatus.Allowed)
            return;

        // It doesn't make any sense for a non-player to send in-game OOC messages, whereas non-players may be sending
        // in-game IC messages.
        if (player?.AttachedEntity is not { Valid: true } entity || source != entity)
            return;
            */

        // CHAT-TODO: Will be handled by markup
        //message = SanitizeInGameOOCMessage(message);

        // CHAT-TODO: Handled by backup
        /*
        var sendType = type;
        // If dead player LOOC is disabled, unless you are an admin with Moderator perms, send dead messages to dead chat
        if ((_adminManager.IsAdmin(player) && _adminManager.HasAdminFlag(player, AdminFlags.Moderator)) // Override if admin
            || _deadLoocEnabled
            || (!HasComp<GhostComponent>(source) && !_mobStateSystem.IsDead(source))) // Check that player is not dead
        {
        }
        else
            sendType = InGameOOCChatType.Dead;*/

        // If crit player LOOC is disabled, don't send the message at all.
        //if (!_critLoocEnabled && _mobStateSystem.IsCritical(source))
        //    return;

        /*
        switch (sendType)
        {
            case InGameOOCChatType.Dead:
                SendDeadChat(source, player, message, hideChat);
                break;
            case InGameOOCChatType.Looc:
                SendLOOC(source, player, message, hideChat);
                break;
        }*/
    }

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

    // CHAT-TODO: This needs to be incorporated into a supplier
    public string TransformSpeech(EntityUid sender, string message)
    {
        var ev = new TransformSpeechEvent(sender, message);
        RaiseLocalEvent(ev);

        return ev.Message;
    }

    // CHAT-TODO: This needs to be incorporated into a supplier
    public bool CheckIgnoreSpeechBlocker(EntityUid sender, bool ignoreBlocker)
    {
        if (ignoreBlocker)
            return ignoreBlocker;

        var ev = new CheckIgnoreSpeechBlockerEvent(sender, ignoreBlocker);
        RaiseLocalEvent(sender, ev, true);

        return ev.IgnoreBlocker;
    }

    // CHAT-TODO: This needs to be incorporated into a supplier
    private IEnumerable<INetChannel> GetDeadChatClients()
    {
        return Filter.Empty()
            .AddWhereAttachedEntity(HasComp<GhostComponent>)
            .Recipients
            .Union(_adminManager.ActiveAdmins)
            .Select(p => p.Channel);
    }

    // CHAT-TODO: This needs to be incorporated into a supplier
    private string SanitizeMessagePeriod(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;
        // Adds a period if the last character is a letter.
        if (char.IsLetter(message[^1]))
            message += ".";
        return message;
    }

    [ValidatePrototypeId<ReplacementAccentPrototype>]
    public const string ChatSanitize_Accent = "chatsanitize";

    // CHAT-TODO: This needs to be incorporated into a supplier
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


    // CHAT-TODO: This needs to be incorporated into a supplier
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


    // CHAT-TODO: This needs to be incorporated into a supplier
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


    // CHAT-TODO: START OF THE CHAT REFACTOR

    #region Helper Messages

    /// <summary>
    /// Helper function to send a server-sent message to all players.
    /// Text is not escaped with this function.
    /// CHAT-TODO: This one is missing functionality
    /// </summary>
    public void DispatchGlobalAnnouncement(
        string message,
        string? sender = null,
        bool playSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null
    )
    {
        sender ??= Loc.GetString("chat-manager-sender-announcement");

        var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender), ("message", FormattedMessage.EscapeText(message)));

        _chatManager.SendChannelMessage(wrappedMessage, _channelDefaultServerAnnouncementChannel, null, null, null, false);
    }

    /// <summary>
    /// Helper function to send a server-sent message to sessions on a specific station.
    /// Text is not escaped with this function.
    /// CHAT-TODO: This one is missing functionality
    /// </summary>
    public void DispatchStationAnnouncement(
        EntityUid source,
        string message,
        string? sender = null,
        bool playDefaultSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null)
    {
        sender ??= Loc.GetString("chat-manager-sender-announcement");

        var wrappedMessage = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender), ("message", FormattedMessage.EscapeText(message)));
        var station = _stationSystem.GetOwningStation(source);

        if (station == null)
        {
            // you can't make a station announcement without a station
            return;
        }

        if (!EntityManager.TryGetComponent<StationDataComponent>(station, out var stationDataComp)) return;

        var filter = _stationSystem.GetInStation(stationDataComp);

        _chatManager.SendChannelMessage(wrappedMessage, _channelDefaultServerAnnouncementChannel, null, null, filter.Recipients.ToHashSet(), false);
    }

    /// <summary>
    /// Helper function to send a server-sent message to a specific session.
    /// Text is not escaped with this function.
    /// </summary>
    public void DispatchServerMessage(ICommonSession session, string message, bool logMessage = true)
    {
        _chatManager.SendChannelMessage(message, _channelDefaultNotificationChannel, null, null, new HashSet<ICommonSession>() { session }, false, logMessage: logMessage);
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

/// <summary>
///     Raised broadcast in order to transform speech.transmit
/// </summary>
public sealed class TransformSpeechEvent : EntityEventArgs
{
    public EntityUid Sender;
    public string Message;

    public TransformSpeechEvent(EntityUid sender, string message)
    {
        Sender = sender;
        Message = message;
    }
}

public sealed class CheckIgnoreSpeechBlockerEvent : EntityEventArgs
{
    public EntityUid Sender;
    public bool IgnoreBlocker;

    public CheckIgnoreSpeechBlockerEvent(EntityUid sender, bool ignoreBlocker)
    {
        Sender = sender;
        IgnoreBlocker = ignoreBlocker;
    }
}

/// <summary>
///     Raised on an entity when it speaks, either through 'say' or 'whisper'.
/// </summary>
public sealed class EntitySpokeEvent : EntityEventArgs
{
    public readonly EntityUid Source;
    public readonly string Message;
    public readonly string? ObfuscatedMessage; // not null if this was a whisper

    /// <summary>
    ///     If the entity was trying to speak into a radio, this was the channel they were trying to access. If a radio
    ///     message gets sent on this channel, this should be set to null to prevent duplicate messages.
    /// </summary>
    public RadioChannelPrototype? Channel;

    public EntitySpokeEvent(EntityUid source, string message, RadioChannelPrototype? channel, string? obfuscatedMessage)
    {
        Source = source;
        Message = message;
        Channel = channel;
        ObfuscatedMessage = obfuscatedMessage;
    }
}

/// <summary>
///     InGame IC chat is for chat that is specifically ingame (not lobby) but is also in character, i.e. speaking.
/// </summary>
// ReSharper disable once InconsistentNaming
public enum InGameICChatType : byte
{
    Speak,
    Emote,
    Whisper
}

/// <summary>
///     Controls transmission of chat.
/// </summary>
public enum ChatTransmitRange : byte
{
    /// Acts normal, ghosts can hear across the map, etc.
    Normal,
    /// Normal but ghosts are still range-limited.
    GhostRangeLimit,
    /// Hidden from the chat window.
    HideChat,
    /// Ghosts can't hear or see it at all. Regular players can if in-range.
    NoGhosts
}
