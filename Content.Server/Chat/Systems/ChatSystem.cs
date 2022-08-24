using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Ghost.Components;
using Content.Server.Mind.Components;
using Content.Server.Players;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.MobState;
using Content.Shared.ActionBlocker;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

/// <summary>
///     ChatSystem is responsible for in-simulation chat handling, such as whispering, speaking, emoting, etc.
///     ChatSystem depends on ChatManager to actually send the messages.
/// </summary>
public sealed partial class ChatSystem : SharedChatSystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IChatSanitizationManager _sanitizer = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ListeningSystem _listener = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    private const int VoiceRange = 7; // how far voice goes in world units
    private const int WhisperRange = 2; // how far whisper goes in world units
    private const string DefaultAnnouncementSound = "/Audio/Announcements/announce.ogg";

    private bool _loocEnabled = true;
    private bool _deadLoocEnabled = false;
    private readonly bool _adminLoocEnabled = true;

    public override void Initialize()
    {
        InitializeRadio();
        _configurationManager.OnValueChanged(CCVars.LoocEnabled, OnLoocEnabledChanged, true);
        _configurationManager.OnValueChanged(CCVars.DeadLoocEnabled, OnDeadLoocEnabledChanged, true);

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameChange);
    }

    public override void Shutdown()
    {
        ShutdownRadio();
        _configurationManager.UnsubValueChanged(CCVars.LoocEnabled, OnLoocEnabledChanged);
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

    private void OnGameChange(GameRunLevelChangedEvent ev)
    {
        if (_configurationManager.GetCVar(CCVars.OocEnableDuringRound))
            return;

        if (ev.New == GameRunLevel.InRound)
            _configurationManager.SetCVar(CCVars.OocEnabled, false);
        else if (ev.New == GameRunLevel.PostRound)
            _configurationManager.SetCVar(CCVars.OocEnabled, true);
    }

    // ReSharper disable once InconsistentNaming
    public void TrySendInGameICMessage(EntityUid source, string message, InGameICChatType desiredType, bool hideChat,
        IConsoleShell? shell = null, IPlayerSession? player = null)
    {
        if (HasComp<GhostComponent>(source))
        {
            // Ghosts can only send dead chat messages, so we'll forward it to InGame OOC.
            TrySendInGameOOCMessage(source, message, InGameOOCChatType.Dead, hideChat, shell, player);
            return;
        }

        // Sus
        if (player?.AttachedEntity is { Valid: true } entity && source != entity)
        {
            return;
        }

        if (!CanSendInGame(message, shell, player))
            return;

        bool shouldCapitalize = (desiredType != InGameICChatType.Emote);

        message = SanitizeInGameICMessage(source, message, out var emoteStr, shouldCapitalize);

        // Was there an emote in the message? If so, send it.
        if (player != null && emoteStr != message && emoteStr != null)
        {
            SendEntityEmote(source, emoteStr, hideChat);
        }

        // This can happen if the entire string is sanitized out.
        if (string.IsNullOrEmpty(message))
            return;

        // Otherwise, send whatever type.
        switch (desiredType)
        {
            case InGameICChatType.Speak:
                SendEntitySpeak(source, message, hideChat);
                break;
            case InGameICChatType.Whisper:
                SendEntityWhisper(source, message, hideChat);
                break;
            case InGameICChatType.Emote:
                SendEntityEmote(source, message, hideChat);
                break;
        }
    }

    public void TrySendInGameOOCMessage(EntityUid source, string message, InGameOOCChatType type, bool hideChat,
        IConsoleShell? shell = null, IPlayerSession? player = null)
    {
        if (!CanSendInGame(message, shell, player))
            return;

        // It doesn't make any sense for a non-player to send in-game OOC messages, whereas non-players may be sending
        // in-game IC messages.
        if (player?.AttachedEntity is not { Valid: true } entity || source != entity)
            return;

        message = SanitizeInGameOOCMessage(message);

        var sendType = type;
        // If dead player LOOC is disabled, unless you are an aghost, send dead messages to dead chat
        if (!_adminManager.IsAdmin(player) && !_deadLoocEnabled &&
            (HasComp<GhostComponent>(source) || _mobStateSystem.IsDead(source)))
            sendType = InGameOOCChatType.Dead;

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

    /// <summary>
    /// Dispatches an announcement to all.
    /// </summary>
    /// <param name="message">The contents of the message</param>
    /// <param name="sender">The sender (Communications Console in Communications Console Announcement)</param>
    /// <param name="playSound">Play the announcement sound</param>
    /// <param name="colorOverride">Optional color for the announcement message</param>
    public void DispatchGlobalAnnouncement(string message, string sender = "Central Command",
        bool playSound = true, SoundSpecifier? announcementSound = null, Color? colorOverride = null)
    {
        var messageWrap = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender));
        _chatManager.ChatMessageToAll(ChatChannel.Radio, message, messageWrap, colorOverride);
        if (playSound)
        {
            SoundSystem.Play(announcementSound?.GetSound() ?? DefaultAnnouncementSound, Filter.Broadcast(), AudioParams.Default.WithVolume(-2f));
        }
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Global station announcement from {sender}: {message}");
    }

    /// <summary>
    /// Dispatches an announcement on a specific station
    /// </summary>
    /// <param name="source">The entity making the announcement (used to determine the station)</param>
    /// <param name="message">The contents of the message</param>
    /// <param name="sender">The sender (Communications Console in Communications Console Announcement)</param>
    /// <param name="playDefaultSound">Play the announcement sound</param>
    /// <param name="colorOverride">Optional color for the announcement message</param>
    public void DispatchStationAnnouncement(EntityUid source, string message, string sender = "Central Command",
        bool playDefaultSound = true, SoundSpecifier? announcementSound = null, Color? colorOverride = null)
    {
        var messageWrap = Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender));
        var station = _stationSystem.GetOwningStation(source);

        if (station == null)
        {
            // you can't make a station announcement without a station
            return;
        }

        if (!EntityManager.TryGetComponent<StationDataComponent>(station, out var stationDataComp)) return;

        var filter = _stationSystem.GetInStation(stationDataComp);

        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, messageWrap, source, false, colorOverride);

        if (playDefaultSound)
        {
            SoundSystem.Play(announcementSound?.GetSound() ?? DefaultAnnouncementSound, filter, AudioParams.Default.WithVolume(-2f));
        }

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Station Announcement on {station} from {sender}: {message}");
    }

    #endregion

    #region Private API

    private void SendEntitySpeak(EntityUid source, string originalMessage, bool hideChat = false)
    {
        if (!_actionBlocker.CanSpeak(source))
            return;

        var (message, channel) = GetRadioPrefix(source, originalMessage);

        if (channel != null)
        {
            _listener.PingListeners(source, message, channel);
            SendEntityWhisper(source, message, hideChat);
            return;
        }

        message = TransformSpeech(source, message);
        if (message.Length == 0)
            return;

        var messageWrap = Loc.GetString("chat-manager-entity-say-wrap-message",
            ("entityName", Name(source)));

        SendInVoiceRange(ChatChannel.Local, message, messageWrap, source, hideChat);
        _listener.PingListeners(source, message, null);

        var ev = new EntitySpokeEvent(message);
        RaiseLocalEvent(source, ev);

        // To avoid logging any messages sent by entities that are not players, like vendors, cloning, etc.
        if (!TryComp(source, out ActorComponent? mind))
            return;

        if (originalMessage == message)
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {ToPrettyString(source):user}: {originalMessage}.");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {ToPrettyString(source):user}, original: {originalMessage}, transformed: {message}.");
    }

    private void SendEntityWhisper(EntityUid source, string originalMessage, bool hideChat = false)
    {
        if (!_actionBlocker.CanSpeak(source))
            return;

        var message = TransformSpeech(source, originalMessage);
        if (message.Length == 0)
            return;

        var obfuscatedMessage = ObfuscateMessageReadability(message, 0.2f);

        var transformSource = Transform(source);
        var sourceCoords = transformSource.Coordinates;
        var messageWrap = Loc.GetString("chat-manager-entity-whisper-wrap-message",
            ("entityName", Name(source)));

        var xforms = GetEntityQuery<TransformComponent>();
        var ghosts = GetEntityQuery<GhostComponent>();

        var sessions = new List<ICommonSession>();
        ClientDistanceToList(source, VoiceRange, sessions);

        // Whisper needs these special calculations, since it can obfuscate the message.
        foreach (var session in sessions)
        {
            if (session.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            var transformEntity = xforms.GetComponent(playerEntity);

            if (sourceCoords.InRange(EntityManager, transformEntity.Coordinates, WhisperRange) ||
                ghosts.HasComponent(playerEntity))
            {
                _chatManager.ChatMessageToOne(ChatChannel.Whisper, message, messageWrap, source, hideChat, session.ConnectedClient);
            }
            else
            {
                _chatManager.ChatMessageToOne(ChatChannel.Whisper, obfuscatedMessage, messageWrap, source, hideChat,
                    session.ConnectedClient);
            }
        }

        var ev = new EntitySpokeEvent(message);
        RaiseLocalEvent(source, ev, false);

        if (originalMessage == message)
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Whisper from {ToPrettyString(source):user}: {originalMessage}.");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Whisper from {ToPrettyString(source):user}, original: {originalMessage}, transformed: {message}.");
    }

    private void SendEntityEmote(EntityUid source, string action, bool hideChat)
    {
        if (!_actionBlocker.CanEmote(source)) return;

        // Emotes use Identity.Name, since it doesn't actually involve your voice at all.
        var messageWrap = Loc.GetString("chat-manager-entity-me-wrap-message",
            ("entityName", Identity.Name(source, EntityManager)));

        SendInVoiceRange(ChatChannel.Emotes, action, messageWrap, source, hideChat);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Emote from {ToPrettyString(source):user}: {action}");
    }

    // ReSharper disable once InconsistentNaming
    private void SendLOOC(EntityUid source, IPlayerSession player, string message, bool hideChat)
    {
        if (_adminManager.IsAdmin(player))
        {
            if (!_adminLoocEnabled) return;
        }
        else if (!_loocEnabled) return;
        var messageWrap = Loc.GetString("chat-manager-entity-looc-wrap-message",
            ("entityName", Identity.Name(source, EntityManager)));

        SendInVoiceRange(ChatChannel.LOOC, message, messageWrap, source, hideChat);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"LOOC from {player:Player}: {message}");
    }

    private void SendDeadChat(EntityUid source, IPlayerSession player, string message, bool hideChat)
    {
        var clients = GetDeadChatClients();
        var playerName = Name(source);
        string messageWrap;
        if (_adminManager.IsAdmin(player))
        {
            messageWrap = Loc.GetString("chat-manager-send-admin-dead-chat-wrap-message",
                ("adminChannelName", Loc.GetString("chat-manager-admin-channel-name")),
                ("userName", player.ConnectedClient.UserName));
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Admin dead chat from {player:Player}: {message}");
        }
        else
        {
            messageWrap = Loc.GetString("chat-manager-send-dead-chat-wrap-message",
                ("deadChannelName", Loc.GetString("chat-manager-dead-channel-name")),
                ("playerName", (playerName)));
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Dead chat from {player:Player}: {message}");
        }

        _chatManager.ChatMessageToMany(ChatChannel.Dead, message, messageWrap, source, hideChat, clients.ToList());

    }
    #endregion

    #region Utility

    /// <summary>
    ///     Sends a chat message to the given players in range of the source entity.
    /// </summary>
    private void SendInVoiceRange(ChatChannel channel, string message, string messageWrap, EntityUid source, bool hideChat)
    {
        var sessions = new List<ICommonSession>();
        ClientDistanceToList(source, VoiceRange, sessions);
        _chatManager.ChatMessageToMany(channel, message, messageWrap, source, hideChat, sessions.Select(s => s.ConnectedClient).ToList());
    }

    /// <summary>
    ///     Returns true if the given player is 'allowed' to send the given message, false otherwise.
    /// </summary>
    private bool CanSendInGame(string message, IConsoleShell? shell = null, IPlayerSession? player = null)
    {
        // Non-players don't have to worry about these restrictions.
        if (player == null)
            return true;

        var mindComponent = player.ContentData()?.Mind;

        if (mindComponent == null)
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
    private string SanitizeInGameICMessage(EntityUid source, string message, out string? emoteStr, bool capitalize = true)
    {
        var newMessage = message.Trim();
        if (capitalize)
            newMessage = SanitizeMessageCapital(source, newMessage);
        newMessage = FormattedMessage.EscapeText(newMessage);

        _sanitizer.TrySanitizeOutSmilies(newMessage, source, out newMessage, out emoteStr);

        return newMessage;
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
        RaiseLocalEvent(ev);

        return ev.Message;
    }

    private IEnumerable<INetChannel> GetDeadChatClients()
    {
        return Filter.Empty()
            .AddWhereAttachedEntity(HasComp<GhostComponent>)
            .Recipients
            .Union(_adminManager.ActiveAdmins)
            .Select(p => p.ConnectedClient);
    }

    private string SanitizeMessageCapital(EntityUid source, string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;
        // Capitalize first letter
        message = message[0].ToString().ToUpper() + message.Remove(0, 1);
        return message;
    }

    private void ClientDistanceToList(EntityUid source, int voiceRange, List<ICommonSession> playerSessions)
    {
        var ghosts = GetEntityQuery<GhostComponent>();
        var xforms = GetEntityQuery<TransformComponent>();

        var transformSource = xforms.GetComponent(source);
        var sourceMapId = transformSource.MapID;
        var sourceCoords = transformSource.Coordinates;

        foreach (var player in _playerManager.Sessions)
        {
            if (player.AttachedEntity is not {Valid: true} playerEntity)
                continue;

            var transformEntity = xforms.GetComponent(playerEntity);

            if (transformEntity.MapID != sourceMapId ||
                !ghosts.HasComponent(playerEntity) &&
                !sourceCoords.InRange(EntityManager, transformEntity.Coordinates, voiceRange))
                continue;

            playerSessions.Add(player);
        }
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

    #endregion
}

/// <summary>
///     Raised broadcast in order to transform speech.
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

/// <summary>
///     Raised on an entity when it speaks, either through 'say' or 'whisper'.
/// </summary>
public sealed class EntitySpokeEvent : EntityEventArgs
{
    public string Message;

    public EntitySpokeEvent(string message)
    {
        Message = message;
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
///     InGame OOC chat is for chat that is specifically ingame (not lobby) but is OOC, like deadchat or LOOC.
/// </summary>
public enum InGameOOCChatType : byte
{
    Looc,
    Dead
}
