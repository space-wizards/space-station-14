using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.Chat.Systems;
using Content.Server.MoMMI;
using Content.Server.Players.RateLimiting;
using Content.Server.Preferences.Managers;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Managers;

/// <summary>
///     Dispatches chat messages to clients.
/// </summary>
internal sealed partial class ChatManager : IChatManager
{
    private static readonly Dictionary<string, string> PatronOocColors = new()
    {
        // I had plans for multiple colors and those went nowhere so...
        { "nuclear_operative", "#aa00ff" },
        { "syndicate_agent", "#aa00ff" },
        { "revolutionary", "#aa00ff" }
    };

    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IMoMMILink _mommiLink = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly INetConfigurationManager _netConfigManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly PlayerRateLimitManager _rateLimitManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;
    [Dependency] private readonly ILogManager _logMan = default!;
    [Dependency] private readonly ILocalizationManager _loc = default!;

    /// <summary>
    /// The maximum length a player-sent message can be sent
    /// </summary>
    public int MaxMessageLength => _configurationManager.GetCVar(CCVars.ChatMaxMessageLength);

    private bool _oocEnabled = true;
    private bool _adminOocEnabled = true;

    private readonly Dictionary<NetUserId, ChatUser> _players = new();
    private ISawmill _log = default!;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgChatMessage>();
        _netManager.RegisterNetMessage<MsgDeleteChatMessagesBy>();
        _netManager.RegisterNetMessage<RequestChatMessage>(OnRequestChat);
        _log = _logMan.GetSawmill("chat");

        _configurationManager.OnValueChanged(CCVars.OocEnabled, OnOocEnabledChanged, true);
        _configurationManager.OnValueChanged(CCVars.AdminOocEnabled, OnAdminOocEnabledChanged, true);

        RegisterRateLimits();
    }

    private void OnRequestChat(RequestChatMessage msg)
    {
        if (_playerMan.TryGetSessionByChannel(msg.MsgChannel, out var session))
            RequestChat(session, msg.Text, msg.Channel);
    }

    /// <inheritdoc/>
    public void RequestChat(ICommonSession session, string text, ChatSelectChannel channel)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        _log.Info($"{session.Name} - {channel}: {text}");

        // entity independent channels
        switch (channel)
        {
            case ChatSelectChannel.OOC:
                TrySendOOCMessage(session, text, OOCChatType.OOC);
                return;

            case ChatSelectChannel.Admin:
                if (_adminManager.HasAdminFlag(session, AdminFlags.Adminchat))
                    TrySendOOCMessage(session, text, OOCChatType.Admin);
                return;
        }

        if (session.Status != SessionStatus.InGame
            || session.AttachedEntity is not { } ent
            || !_entityManager.EntityExists(ent))
        {
            var msg = _loc.GetString("chat-manager-no-entity", ("channel", channel));
            DispatchServerMessage(session, msg, true);
            return;
        }

        var sys = _entityManager.SystemOrNull<ChatSystem>();
        switch (channel)
        {
            case ChatSelectChannel.LOOC:
                sys?.TrySendInGameOOCMessage(ent, text, InGameOOCChatType.Looc, false, null, session);
                break;
            case ChatSelectChannel.Dead:
                if (_entityManager.HasComponent<GhostComponent>(ent) || _adminManager.HasAdminFlag(session, AdminFlags.Admin))
                    sys?.TrySendInGameOOCMessage(ent, text, InGameOOCChatType.Dead, false, null, session);
                else
                    DispatchServerMessage(session, _loc.GetString("chat-manager-not-dead"), true);
                break;
            case ChatSelectChannel.Emotes:
                sys?.TrySendInGameICMessage(ent, text, InGameICChatType.Emote, ChatTransmitRange.Normal, false, null, session);
                break;
            case ChatSelectChannel.Whisper:
                sys?.TrySendInGameICMessage(ent, text, InGameICChatType.Whisper, ChatTransmitRange.Normal, false, null, session);
                break;
            // TODO separate radio and say
            case ChatSelectChannel.Radio:
            case ChatSelectChannel.Local:
                sys?.TrySendInGameICMessage(ent, text, InGameICChatType.Speak, ChatTransmitRange.Normal, false, null, session);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
        }
    }

    private void OnOocEnabledChanged(bool val)
    {
        if (_oocEnabled == val) return;

        _oocEnabled = val;
        DispatchServerAnnouncement(_loc.GetString(val ? "chat-manager-ooc-chat-enabled-message" : "chat-manager-ooc-chat-disabled-message"));
    }

    private void OnAdminOocEnabledChanged(bool val)
    {
        if (_adminOocEnabled == val) return;

        _adminOocEnabled = val;
        DispatchServerAnnouncement(_loc.GetString(val ? "chat-manager-admin-ooc-chat-enabled-message" : "chat-manager-admin-ooc-chat-disabled-message"));
    }

        public void DeleteMessagesBy(NetUserId uid)
        {
            if (!_players.TryGetValue(uid, out var user))
                return;

        var msg = new MsgDeleteChatMessagesBy { Key = user.Key, Entities = user.Entities };
        _netManager.ServerSendToAll(msg);
    }

    [return: NotNullIfNotNull(nameof(author))]
    public ChatUser? EnsurePlayer(NetUserId? author)
    {
        if (author == null)
            return null;

        ref var user = ref CollectionsMarshal.GetValueRefOrAddDefault(_players, author.Value, out var exists);
        if (!exists || user == null)
            user = new ChatUser(_players.Count);

        return user;
    }

    #region Server Announcements

    public void DispatchServerAnnouncement(string message, Color? colorOverride = null)
    {
        var wrappedMessage = _loc.GetString("chat-manager-server-wrap-message", ("message", FormattedMessage.EscapeText(message)));
        ChatMessageToAll(ChatChannel.Server, message, wrappedMessage, EntityUid.Invalid, hideChat: false, recordReplay: true, colorOverride: colorOverride);
        Logger.InfoS("SERVER", message);

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Server announcement: {message}");
    }

    public void DispatchServerMessage(ICommonSession player, string message, bool suppressLog = false)
    {
        var wrappedMessage = _loc.GetString("chat-manager-server-wrap-message", ("message", FormattedMessage.EscapeText(message)));
        ChatMessageToOne(ChatChannel.Server, message, wrappedMessage, default, false, player.Channel);

        if (!suppressLog)
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Server message to {player:Player}: {message}");
    }

    public void SendAdminAnnouncement(string message, AdminFlags? flagBlacklist, AdminFlags? flagWhitelist)
    {
        var clients = _adminManager.ActiveAdmins.Where(p =>
        {
            var adminData = _adminManager.GetAdminData(p);

            DebugTools.AssertNotNull(adminData);

            if (adminData == null)
                return false;

            if (flagBlacklist != null && adminData.HasFlag(flagBlacklist.Value))
                return false;

            return flagWhitelist == null || adminData.HasFlag(flagWhitelist.Value);

        }).Select(p => p.Channel);

        var wrappedMessage = _loc.GetString("chat-manager-send-admin-announcement-wrap-message",
            ("adminChannelName", _loc.GetString("chat-manager-admin-channel-name")), ("message", FormattedMessage.EscapeText(message)));

        ChatMessageToMany(ChatChannel.Admin, message, wrappedMessage, default, false, true, clients);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Admin announcement: {message}");
    }

    public void SendAdminAnnouncementMessage(ICommonSession player, string message, bool suppressLog = true)
    {
        var wrappedMessage = _loc.GetString("chat-manager-send-admin-announcement-wrap-message",
            ("adminChannelName", _loc.GetString("chat-manager-admin-channel-name")),
            ("message", FormattedMessage.EscapeText(message)));
        ChatMessageToOne(ChatChannel.Admin, message, wrappedMessage, default, false, player.Channel);
    }

    public void SendAdminAlert(string message)
    {
        var clients = _adminManager.ActiveAdmins.Select(p => p.Channel);

        var wrappedMessage = _loc.GetString("chat-manager-send-admin-announcement-wrap-message",
            ("adminChannelName", _loc.GetString("chat-manager-admin-channel-name")), ("message", FormattedMessage.EscapeText(message)));

        ChatMessageToMany(ChatChannel.AdminAlert, message, wrappedMessage, default, false, true, clients);
    }

    public void SendAdminAlert(EntityUid player, string message)
    {
        var mindSystem = _entityManager.System<SharedMindSystem>();
        if (!mindSystem.TryGetMind(player, out var mindId, out var mind))
        {
            SendAdminAlert(message);
            return;
        }

        var adminSystem = _entityManager.System<AdminSystem>();
        var antag = mind.UserId != null && (adminSystem.GetCachedPlayerInfo(mind.UserId.Value)?.Antag ?? false);

        SendAdminAlert($"{mind.Session?.Name}{(antag ? " (ANTAG)" : "")} {message}");
    }

    public void SendHookOOC(string sender, string message)
    {
        if (!_oocEnabled && _configurationManager.GetCVar(CCVars.DisablingOOCDisablesRelay))
        {
            return;
        }
        var wrappedMessage = _loc.GetString("chat-manager-send-hook-ooc-wrap-message", ("senderName", sender), ("message", FormattedMessage.EscapeText(message)));
        ChatMessageToAll(ChatChannel.OOC, message, wrappedMessage, source: EntityUid.Invalid, hideChat: false, recordReplay: true);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Hook OOC from {sender}: {message}");
    }

    #endregion

    #region Public OOC Chat API

    /// <summary>
    ///     Called for a player to attempt sending an OOC, out-of-game. message.
    /// </summary>
    /// <param name="player">The player sending the message.</param>
    /// <param name="message">The message.</param>
    /// <param name="type">The type of message.</param>
    public void TrySendOOCMessage(ICommonSession player, string message, OOCChatType type)
    {
        if (HandleRateLimit(player) != RateLimitStatus.Allowed)
            return;

        // Check if message exceeds the character limit
        if (ExceedsCharacterLimit(player, message))
            return;

        switch (type)
        {
            case OOCChatType.OOC:
                SendOOC(player, message);
                break;
            case OOCChatType.Admin:
                SendAdminChat(player, message);
                break;
        }
    }

    #endregion

    #region Private API

    private void SendOOC(ICommonSession player, string message)
    {
        if (_adminManager.IsAdmin(player))
        {
            if (!_adminOocEnabled)
            {
                return;
            }
        }
        else if (!_oocEnabled)
        {
            return;
        }

        Color? colorOverride = null;
        var wrappedMessage = _loc.GetString("chat-manager-send-ooc-wrap-message", ("playerName",player.Name), ("message", FormattedMessage.EscapeText(message)));
        if (_adminManager.HasAdminFlag(player, AdminFlags.Admin))
        {
            var prefs = _preferencesManager.GetPreferences(player.UserId);
            colorOverride = prefs.AdminOOCColor;
        }
        if (  _netConfigManager.GetClientCVar(player.Channel, CCVars.ShowOocPatronColor) && player.Channel.UserData.PatronTier is { } patron && PatronOocColors.TryGetValue(patron, out var patronColor))
        {
            wrappedMessage = _loc.GetString("chat-manager-send-ooc-patron-wrap-message", ("patronColor", patronColor),("playerName", player.Name), ("message", FormattedMessage.EscapeText(message)));
        }

        //TODO: player.Name color, this will need to change the structure of the MsgChatMessage
        ChatMessageToAll(ChatChannel.OOC, message, wrappedMessage, EntityUid.Invalid, hideChat: false, recordReplay: true, colorOverride: colorOverride, author: player.UserId);
        _mommiLink.SendOOCMessage(player.Name, message.Replace("@", "\\@").Replace("<", "\\<").Replace("/", "\\/")); // @ and < are both problematic for discord due to pinging. / is sanitized solely to kneecap links to murder embeds via blunt force
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"OOC from {player:Player}: {message}");
    }

    private void SendAdminChat(ICommonSession player, string message)
    {
        if (!_adminManager.IsAdmin(player))
        {
            _adminLogger.Add(LogType.Chat, LogImpact.Extreme, $"{player:Player} attempted to send admin message but was not admin");
            return;
        }

        var clients = _adminManager.ActiveAdmins.Select(p => p.Channel);
        var wrappedMessage = _loc.GetString("chat-manager-send-admin-chat-wrap-message",
                                        ("adminChannelName", _loc.GetString("chat-manager-admin-channel-name")),
                                        ("playerName", player.Name), ("message", FormattedMessage.EscapeText(message)));

        foreach (var client in clients)
        {
            var isSource = client != player.Channel;
            ChatMessageToOne(ChatChannel.AdminChat,
                message,
                wrappedMessage,
                default,
                false,
                client,
                audioPath: isSource ? _netConfigManager.GetClientCVar(client, CCVars.AdminChatSoundPath) : default,
                audioVolume: isSource ? _netConfigManager.GetClientCVar(client, CCVars.AdminChatSoundVolume) : default,
                author: player.UserId);
        }

        _adminLogger.Add(LogType.Chat, $"Admin chat from {player:Player}: {message}");
    }

    #endregion

    #region Utility

    public void ChatMessageToOne(ChatChannel channel, string message, string wrappedMessage, EntityUid source, bool hideChat, INetChannel client, Color? colorOverride = null, bool recordReplay = false, string? audioPath = null, float audioVolume = 0, NetUserId? author = null)
    {
        var user = author == null ? null : EnsurePlayer(author);
        var netSource = _entityManager.GetNetEntity(source);
        user?.AddEntity(netSource);

        var msg = new ChatMessage(channel, message, wrappedMessage, netSource, user?.Key, hideChat, colorOverride, audioPath, audioVolume);
        _netManager.ServerSendMessage(new MsgChatMessage() { Message = msg }, client);

        if (!recordReplay)
            return;

        if ((channel & ChatChannel.AdminRelated) == 0 ||
            _configurationManager.GetCVar(CCVars.ReplayRecordAdminChat))
        {
            _replay.RecordServerMessage(msg);
        }
    }

    public void ChatMessageToMany(ChatChannel channel, string message, string wrappedMessage, EntityUid source, bool hideChat, bool recordReplay, IEnumerable<INetChannel> clients, Color? colorOverride = null, string? audioPath = null, float audioVolume = 0, NetUserId? author = null)
        => ChatMessageToMany(channel, message, wrappedMessage, source, hideChat, recordReplay, clients.ToList(), colorOverride, audioPath, audioVolume, author);

    public void ChatMessageToMany(ChatChannel channel, string message, string wrappedMessage, EntityUid source, bool hideChat, bool recordReplay, List<INetChannel> clients, Color? colorOverride = null, string? audioPath = null, float audioVolume = 0, NetUserId? author = null)
    {
        var user = author == null ? null : EnsurePlayer(author);
        var netSource = _entityManager.GetNetEntity(source);
        user?.AddEntity(netSource);

        var msg = new ChatMessage(channel, message, wrappedMessage, netSource, user?.Key, hideChat, colorOverride, audioPath, audioVolume);
        _netManager.ServerSendToMany(new MsgChatMessage() { Message = msg }, clients);

        if (!recordReplay)
            return;

        if ((channel & ChatChannel.AdminRelated) == 0 ||
            _configurationManager.GetCVar(CCVars.ReplayRecordAdminChat))
        {
            _replay.RecordServerMessage(msg);
        }
    }

    public void ChatMessageToManyFiltered(Filter filter, ChatChannel channel, string message, string wrappedMessage, EntityUid source,
        bool hideChat, bool recordReplay, Color? colorOverride = null, string? audioPath = null, float audioVolume = 0)
    {
        if (!recordReplay && !filter.Recipients.Any())
            return;

        var clients = new List<INetChannel>();
        foreach (var recipient in filter.Recipients)
        {
            clients.Add(recipient.Channel);
        }

        ChatMessageToMany(channel, message, wrappedMessage, source, hideChat, recordReplay, clients, colorOverride, audioPath, audioVolume);
    }

    public void ChatMessageToAll(ChatChannel channel, string message, string wrappedMessage, EntityUid source, bool hideChat, bool recordReplay, Color? colorOverride = null, string? audioPath = null, float audioVolume = 0, NetUserId? author = null)
    {
        var user = author == null ? null : EnsurePlayer(author);
        var netSource = _entityManager.GetNetEntity(source);
        user?.AddEntity(netSource);

        var msg = new ChatMessage(channel, message, wrappedMessage, netSource, user?.Key, hideChat, colorOverride, audioPath, audioVolume);
        _netManager.ServerSendToAll(new MsgChatMessage() { Message = msg });

        if (!recordReplay)
            return;

        if ((channel & ChatChannel.AdminRelated) == 0 ||
            _configurationManager.GetCVar(CCVars.ReplayRecordAdminChat))
        {
            _replay.RecordServerMessage(msg);
        }
    }

    /// <summary>
    /// Checks if a message exceeds the character limit. If ti does, it will notify the player.
    /// </summary>
    public bool ExceedsCharacterLimit(ICommonSession? player, string message)
    {
        // Non-players don't need to be checked.
        if (player == null)
            return false;

        // Check if message exceeds the character limit if the sender is a player
        if (message.Length <= MaxMessageLength)
            return false;

        var feedback = _loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", MaxMessageLength));
        DispatchServerMessage(player, feedback);
        return true;
    }

    #endregion
}

public enum OOCChatType : byte
{
    OOC,
    Admin
}
