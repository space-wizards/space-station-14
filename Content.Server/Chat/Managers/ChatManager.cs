using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.Chat.V2.Repository;
using Content.Server.MoMMI;
using Content.Server.Players.RateLimiting;
using Content.Server.Preferences.Managers;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Database;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
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
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IMoMMILink _mommiLink = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly INetConfigurationManager _netConfigManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PlayerRateLimitManager _rateLimitManager = default!;
    [Dependency] private readonly IChatRepository _chatRepository = default!;

    /// <summary>
    /// The maximum length a player-sent message can be sent
    /// </summary>
    public int MaxMessageLength => _configurationManager.GetCVar(CCVars.ChatMaxMessageLength);

    private bool _oocEnabled = true;
    private bool _adminOocEnabled = true;

    private readonly Dictionary<NetUserId, ChatUser> _players = new();

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgChatMessage>();
        _netManager.RegisterNetMessage<MsgDeleteChatMessagesBy>();

        _configurationManager.OnValueChanged(CCVars.OocEnabled, OnOocEnabledChanged, true);
        _configurationManager.OnValueChanged(CCVars.AdminOocEnabled, OnAdminOocEnabledChanged, true);

        RegisterRateLimits();
    }

    private void OnOocEnabledChanged(bool val)
    {
        if (_oocEnabled == val) return;

        _oocEnabled = val;
        DispatchServerAnnouncement(Loc.GetString(val ? "chat-manager-ooc-chat-enabled-message" : "chat-manager-ooc-chat-disabled-message"));
    }

    private void OnAdminOocEnabledChanged(bool val)
    {
        if (_adminOocEnabled == val) return;

        _adminOocEnabled = val;
        DispatchServerAnnouncement(Loc.GetString(val ? "chat-manager-admin-ooc-chat-enabled-message" : "chat-manager-admin-ooc-chat-disabled-message"));
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

    #region Public Chat API

    public void SendAdminAnnouncement(string message, AdminFlags? flags = null)
    {
        if (flags != null)
        {
            var clients = _adminManager.ActiveAdmins.Where(p =>
            {
                var adminData = _adminManager.GetAdminData(p);

                DebugTools.AssertNotNull(adminData);

                if (adminData == null)
                    return false;

                if ((adminData.Flags & flags) == 0)
                    return false;

                return true;

            });

            SendChannelMessage(message, "AdminChat", null, null, clients.ToHashSet(), false);
        }
        else
        {
            SendChannelMessage(message, "AdminAlert", null, null, null, false);
        }
    }

    public void SendAdminAnnouncementMessage(ICommonSession player, string message, bool suppressLog = true)
    {
        var wrappedMessage = Loc.GetString("chat-manager-send-admin-announcement-wrap-message",
            ("adminChannelName", Loc.GetString("chat-manager-admin-channel-name")),
            ("message", FormattedMessage.EscapeText(message)));
        SendChannelMessage(wrappedMessage, "AdminAlert", null, null, new HashSet<ICommonSession>() { player }, logMessage: suppressLog);
    }

    public void SendAdminAlert(string message)
    {
        SendChannelMessage(message, "AdminAlert", null, null);
    }

    // CHAT-TODO: This can probably be refactored into using markup tags.
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
        // Probably should be formatted using markup, but since it's a hook I'm a bit uncertain
        var wrappedMessage = Loc.GetString("chat-manager-send-hook-ooc-wrap-message", ("senderName", sender), ("message", FormattedMessage.EscapeText(message)));
        SendChannelMessage(wrappedMessage, "OOC", null, null);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Hook OOC from {sender}: {message}");
    }

    public void DispatchServerAnnouncement(string message)
    {
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", FormattedMessage.EscapeText(message)));
        SendChannelMessage(wrappedMessage, "Server", null, null);
        Logger.InfoS("SERVER", message);

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Server announcement: {message}");
    }

    public void DispatchServerMessage(ICommonSession player, string message, bool suppressLog = false)
    {
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", FormattedMessage.EscapeText(message)));
        SendChannelMessage(wrappedMessage, "Server", null, null, new HashSet<ICommonSession>() { player }, logMessage: suppressLog);

        if (!suppressLog)
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Server message to {player:Player}: {message}");
    }

    #endregion

    #region Base Chat Functionality

    public void SendChannelMessage(
        string message,
        ProtoId<CommunicationChannelPrototype> communicationChannel,
        ICommonSession? senderSession,
        EntityUid? senderEntity,
        HashSet<ICommonSession>? targetSessions = null,
        bool escapeText = true,
        ChatMessageContext? channelParameters = null,
        bool logMessage = true
    )
    {
        var formattedMessage = escapeText
            ? FormattedMessage.FromMarkupPermissive(message, out _)
            : FormattedMessage.FromUnformatted(FormattedMessage.EscapeText(message));

        SendChannelMessage(formattedMessage, communicationChannel, senderSession, senderEntity, targetSessions, channelParameters);
    }

    public void SendChannelMessage(
        FormattedMessage message,
        string communicationChannel,
        ICommonSession? senderSession,
        EntityUid? senderEntity,
        HashSet<ICommonSession>? targetSessions = null,
        ChatMessageContext? channelParameters = null,
        bool logMessage = true
    )
    {
        _prototypeManager.TryIndex<CommunicationChannelPrototype>(communicationChannel, out var proto);

        if (proto != null)
        {
            var wrapped = _chatRepository.Add(
                message,
                proto,
                senderSession,
                senderEntity,
                null,
                targetSessions,
                channelParameters
            );
            if(wrapped == null)
                return;

            SendChannelMessage(wrapped, logMessage);
        }
    }
    
    #endregion

    #region Utility

    public void ChatFormattedMessageToHashset(
        uint messageId,
        FormattedMessage message,
        CommunicationChannelPrototype channel,
        IEnumerable<INetChannel> clients,
        EntityUid? source,
        bool hideChat,
        bool recordReplay,
        NetUserId? author = null
    )
    {
        var user = author == null ? null : EnsurePlayer(author);
        var netSource = _entityManager.GetNetEntity(source ?? default);
        user?.AddEntity(netSource);

        if (string.IsNullOrEmpty(message.ToMarkup()))
            return;

        var msg = new ChatMessage(messageId, channel, message, netSource, user?.Key, hideChat);
        _netManager.ServerSendToMany(new MsgChatMessage() { Message = msg }, clients.ToList());

        if (!recordReplay)
            return;

        if ((channel.ChatFilter & ChatChannelFilter.AdminRelated) == 0 ||
            _configurationManager.GetCVar(CCVars.ReplayRecordAdminChat))
        {
            _replay.RecordServerMessage(msg);
        }
    }

    // CHAT-TODO: Figure this one out too
    public bool MessageCharacterLimit(ICommonSession? player, string message)
    {
        var isOverLength = false;

        // Non-players don't need to be checked.
        if (player == null)
            return false;

        // Check if message exceeds the character limit if the sender is a player
        if (message.Length > MaxMessageLength)
        {
            var feedback = Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", MaxMessageLength));

            // CHAT-TODO: Figure this one out too
            DispatchServerMessage(player, feedback);

            isOverLength = true;
        }

        return isOverLength;
    }

    #endregion
}


public enum OOCChatType : byte
{
    OOC,
    Admin
}
