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
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Players;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Radio;
using Content.Shared.Speech.Components;
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

/// <summary>
///     This system contains obsolete functions that were utilized in the previous chat version.
///     Ideally these should be phased out eventually, but are here for supporting forks at the moment.
/// </summary>
public sealed partial class ChatSystem
{

    [Obsolete]
    public const int VoiceRange = 10; // how far voice goes in world units
    [Obsolete]
    public const int WhisperClearRange = 2; // how far whisper goes while still being understandable, in world units
    [Obsolete]
    public const int WhisperMuffledRange = 5; // how far whisper goes at all, in world units
    [Obsolete]
    public const string DefaultAnnouncementSound = "/Audio/Announcements/announce.ogg";

    /// <summary>
    ///     Sends an in-character chat message to relevant clients.
    /// </summary>
    /// <param name="source">The entity that is speaking</param>
    /// <param name="message">The message being spoken or emoted</param>
    /// <param name="desiredType">The chat type</param>
    /// <param name="hideChat">Whether or not this message should appear in the chat window</param>
    /// <param name="hideLog">Whether or not this message should appear in the adminlog window</param>
    /// <param name="shell"></param>
    /// <param name="player">The player doing the speaking</param>
    /// <param name="nameOverride">The name to use for the speaking entity. Usually this should just be modified via <see cref="TransformSpeakerNameEvent"/>. If this is set, the event will not get raised.</param>
    [Obsolete("Use SendChannelMessage instead.")]
    public void TrySendInGameICMessage(
        EntityUid source,
        string message,
        InGameICChatType desiredType,
        bool hideChat, bool hideLog = false,
        IConsoleShell? shell = null,
        ICommonSession? player = null, string? nameOverride = null,
        bool checkRadioPrefix = true,
        bool ignoreActionBlocker = false)
    {
        TrySendInGameICMessage(source, message, desiredType, hideChat ? ChatTransmitRange.HideChat : ChatTransmitRange.Normal, hideLog, shell, player, nameOverride, checkRadioPrefix, ignoreActionBlocker);
    }

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
    [Obsolete("Use SendChannelMessage instead.")]
    public void TrySendInGameICMessage(
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
        _chatManager.SendChannelMessage(message, "ICSpeech", player, source);
    }

    [Obsolete("Use SendChannelMessage instead.")]
    public void TrySendInGameOOCMessage(
        EntityUid source,
        string message,
        InGameOOCChatType type,
        bool hideChat,
        IConsoleShell? shell = null,
        ICommonSession? player = null
        )
    {
        _chatManager.SendChannelMessage(message, "LOOC", player, source);
    }

    #region Announcements

    /// <summary>
    /// Dispatches an announcement to all.
    /// </summary>
    /// <param name="message">The contents of the message</param>
    /// <param name="sender">The sender (Communications Console in Communications Console Announcement)</param>
    /// <param name="playSound">Play the announcement sound</param>
    /// <param name="colorOverride">Optional color for the announcement message</param>
    /// CHAT-TODO: This one is missing functionality such as color override and announcement sound
    [Obsolete("Use SendChannelMessage instead.")]
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
    /// Dispatches an announcement to players selected by filter.
    /// </summary>
    /// <param name="filter">Filter to select players who will recieve the announcement</param>
    /// <param name="message">The contents of the message</param>
    /// <param name="source">The entity making the announcement (used to determine the station)</param>
    /// <param name="sender">The sender (Communications Console in Communications Console Announcement)</param>
    /// <param name="playDefaultSound">Play the announcement sound</param>
    /// <param name="announcementSound">Sound to play</param>
    /// <param name="colorOverride">Optional color for the announcement message</param>
    /// CHAT-TODO: This one is missing functionality such as color override and announcement sound
    [Obsolete("Use SendChannelMessage instead.")]
    public void DispatchFilteredAnnouncement(
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
        var station = _stationSystem.GetOwningStation(source);

        if (station == null)
        {
            // you can't make a station announcement without a station
            return;
        }

        if (!EntityManager.TryGetComponent<StationDataComponent>(station, out var stationDataComp)) return;

        _chatManager.SendChannelMessage(wrappedMessage, _channelDefaultServerAnnouncementChannel, null, null, filter.Recipients.ToHashSet(), false);
    }

    /// <summary>
    /// Dispatches an announcement on a specific station
    /// </summary>
    /// <param name="source">The entity making the announcement (used to determine the station)</param>
    /// <param name="message">The contents of the message</param>
    /// <param name="sender">The sender (Communications Console in Communications Console Announcement)</param>
    /// <param name="playDefaultSound">Play the announcement sound</param>
    /// <param name="colorOverride">Optional color for the announcement message</param>
    [Obsolete("Use SendChannelMessage instead.")]
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

        if (playDefaultSound)
        {
            _audio.PlayGlobal(announcementSound?.ToString() ?? DefaultAnnouncementSound, filter, true, AudioParams.Default.WithVolume(-2f));
        }

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Station Announcement on {station} from {sender}: {message}");
    }

    #endregion

    #region Utility

    [Obsolete]
    private enum MessageRangeCheckResult
    {
        Disallowed,
        HideChat,
        Full
    }

    /// <summary>
    ///     If hideChat should be set as far as replays are concerned.
    /// </summary>
    [Obsolete]
    private bool MessageRangeHideChatForReplay(ChatTransmitRange range)
    {
        return range == ChatTransmitRange.HideChat;
    }

    /// <summary>
    ///     Checks if a target as returned from GetRecipients should receive the message.
    ///     Keep in mind data.Range is -1 for out of range observers.
    /// </summary>
    [Obsolete]
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

    [Obsolete("?")]
    public string TransformSpeech(EntityUid sender, string message)
    {
        var ev = new TransformSpeechEvent(sender, message);
        RaiseLocalEvent(ev);

        return ev.Message;
    }

    [Obsolete("?")]
    public bool CheckIgnoreSpeechBlocker(EntityUid sender, bool ignoreBlocker)
    {
        if (ignoreBlocker)
            return ignoreBlocker;

        var ev = new CheckIgnoreSpeechBlockerEvent(sender, ignoreBlocker);
        RaiseLocalEvent(sender, ev, true);

        return ev.IgnoreBlocker;
    }

    [ValidatePrototypeId<ReplacementAccentPrototype>]
    [Obsolete("?")]
    public const string ChatSanitize_Accent = "chatsanitize";

    [Obsolete("?")]
    public string SanitizeMessageReplaceWords(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;

        var msg = message;

        msg = _wordreplacement.ApplyReplacements(msg, ChatSanitize_Accent);

        return msg;
    }

    [Obsolete]
    public readonly record struct ICChatRecipientData(float Range, bool Observer, bool? HideChatOverride = null)
    {
    }

    [Obsolete("?")]
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
///     InGame IC chat is for chat that is specifically ingame (not lobby) but is also in character, i.e. speaking.
/// </summary>
// ReSharper disable once InconsistentNaming
[Obsolete]
public enum InGameICChatType : byte
{
    Speak,
    Emote,
    Whisper
}

/// <summary>
///     InGame OOC chat is for chat that is specifically ingame (not lobby) but is OOC, like deadchat or LOOC.
/// </summary>
[Obsolete]
public enum InGameOOCChatType : byte
{
    Looc,
    Dead
}

/// <summary>
///     Controls transmission of chat.
/// </summary>
[Obsolete]
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
