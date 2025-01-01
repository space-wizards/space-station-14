using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.MoMMI;
using Content.Server.Players.RateLimiting;
using Content.Server.Preferences.Managers;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Database;
using Content.Shared.Mind;
using Content.Shared.Players.RateLimiting;
using Microsoft.Extensions.Logging;
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

    public void SendChannelMessage(string message, string communicationChannel, ICommonSession? senderSession, EntityUid? senderEntity, HashSet<ICommonSession>? targetSessions = null, bool escapeText = true, Dictionary<Enum, object>? supplierParameters = null, bool logMessage = true)
    {
        var formattedMessage = escapeText ? FormattedMessage.FromMarkupPermissive(message, out string? error) : FormattedMessage.FromUnformatted(FormattedMessage.EscapeText(message));

        SendChannelMessage(formattedMessage, communicationChannel, senderSession, senderEntity, targetSessions, supplierParameters);
    }

    public void SendChannelMessage(FormattedMessage message, string communicationChannel, ICommonSession? senderSession, EntityUid? senderEntity, HashSet<ICommonSession>? targetSessions = null, Dictionary<Enum, object>? supplierParameters = null, bool logMessage = true)
    {
        _prototypeManager.TryIndex<CommunicationChannelPrototype>(communicationChannel, out var proto);
        var usedCommsTypes = new List<CommunicationChannelPrototype>();

        if (proto != null)
            SendChannelMessage(message, proto, senderSession, senderEntity, ref usedCommsTypes, targetSessions, supplierParameters);
    }

    public void SendChannelMessage(FormattedMessage message, string communicationChannel, ICommonSession? senderSession, EntityUid? senderEntity, ref List<CommunicationChannelPrototype> usedCommsTypes, HashSet<ICommonSession>? targetSessions = null, Dictionary<Enum, object>? supplierParameters = null, bool logMessage = true)
    {
        _prototypeManager.TryIndex<CommunicationChannelPrototype>(communicationChannel, out var proto);

        if (proto != null)
            SendChannelMessage(message, proto, senderSession, senderEntity, ref usedCommsTypes, targetSessions, supplierParameters);
    }

    /// <summary>
    /// Processes a message with formatting, markup and makes sure it gets sent out to the appropriate sessions/entities as designated by the chosen communication channel.
    /// </summary>
    /// <param name="message">The message that is to be sent.</param>
    /// <param name="communicationChannel">The communication channel prototype to use as a base for how the message should be treated.</param>
    /// <param name="senderSession">The session that sends the message. If null, this means the server is sending the message.</param>
    /// <param name="senderEntity">The entity designated as "sending" the message. If null, the message is coming directly from the session/server.</param>
    /// <param name="usedCommsChannels">Tracks the communication channels used. Helps prevent infinitely recursive messages.</param>
    /// <param name="targetSessions">Any sessions that should be specifically targetted (still needs to comply with channel consume conditions). If you are targetting multiple sessions you should likely use a consumeCollection instead of this.</param>
    /// <param name="supplierParameters">Parameters that may be used by MarkupSuppliers; these are not passed on to the client.</param>
    public void SendChannelMessage(
        FormattedMessage message,
        CommunicationChannelPrototype communicationChannel,
        ICommonSession? senderSession,
        EntityUid? senderEntity,
        ref List<CommunicationChannelPrototype> usedCommsChannels,
        HashSet<ICommonSession>? targetSessions = null,
        Dictionary<Enum, object>? channelParameters = null,
        bool logMessage = true)
    {

        #region Prep-Step

        // This section handles setting up the parameters and any other business that should happen before validation starts.

        // If the comms type is non-repeatable (i.e. the message may only ever be sent ONCE on that comms channel) check for it and block if it has been sent.
        if (communicationChannel.NonRepeatable && usedCommsChannels.Contains(communicationChannel))
            return;

        // Check for rate limiting if it's a client sending the message
        if (senderSession != null && HandleRateLimit(senderSession) != RateLimitStatus.Allowed)
                return;

        // Set the channel parameters, and supply any custom ones if necessary.
        var compiledChannelParameters = communicationChannel.ChannelParameters;
        if (channelParameters != null)
        {
            channelParameters.ToList()
                .ForEach(x => compiledChannelParameters[x.Key] = x.Value);
        }

        // Includes the sender as a parameter for nodes that need it
        if (senderEntity != null)
            compiledChannelParameters[DefaultChannelParameters.SenderEntity] = senderEntity.Value;

        if (senderSession != null)
            compiledChannelParameters[DefaultChannelParameters.SenderSession] = senderSession;

        // Include a random seed based on the message's hashcode.
        // Since the message has yet to be formatted by anything, any child channels should get the same random seed.
        compiledChannelParameters[DefaultChannelParameters.RandomSeed] = message.GetHashCode();

        #endregion

        #region Publisher Validation

        // This section handles validating the publisher based on ChatConditions, and passing on the message should the validation fail.

        if (!communicationChannel.AllowEntitylessMessages && senderEntity == null)
        {
            Logger.Debug("EntitylessMessageNotAllowed");
            return;
        }

        var failedPublishing = false;

        // If senderSession is null, it means the server is sending the message.
        if (senderSession != null)
        {
            var basePublishChatCondition = new ChatCondition(communicationChannel.PublishChatConditions);

            var result = basePublishChatCondition.ProcessCondition(new HashSet<ICommonSession>() { senderSession }, compiledChannelParameters);

            var allowPublish = false;

            if (result.Count > 0)
                allowPublish = true;

            if (!allowPublish)
                failedPublishing = true;
        }

        // We also pass it on to any child channels that should be included.
        var childChannels = HandleChildChannels(communicationChannel, communicationChannel.AlwaysChildCommunicationChannels);
        if (childChannels.Count > 0)
        {
            foreach (var childChannel in childChannels)
            {
                SendChannelMessage(
                    message,
                    childChannel,
                    senderSession,
                    senderEntity,
                    ref usedCommsChannels,
                    targetSessions,
                    channelParameters);
            }
        }

        // If the sender failed the publishing conditions, this attempt a back-up channel.
        // Useful for e.g. making ghosts trying to send LOOC messages fall back to Deadchat instead.
        if (failedPublishing)
        {
            var backupChildChannels = HandleChildChannels(communicationChannel, communicationChannel.BackupChildCommunicationChannels);
            if (backupChildChannels.Count > 0)
            {
                foreach (var backupChildChannel in backupChildChannels)
                {
                    SendChannelMessage(
                        message,
                        backupChildChannel,
                        senderSession,
                        senderEntity,
                        ref usedCommsChannels,
                        targetSessions);
                }
            }
            return;
        }

        //At this point we may assume that the message is publishing; as such, it should be recorded in the usedCommsTypes.
        if (communicationChannel.NonRepeatable)
            usedCommsChannels.Add(communicationChannel);

        #endregion

        #region Consumers

        // This section handles sending out the message to consumers, whether that be sessions or entities.
        // This is done via consume collections; a consume collection contains a list of conditions for sessions and entities,
        // as well as chat modifiers that change the message clientside.

        // Sessions/Entities are processed via the first consume collection they meet the conditions for;
        // therefore, they should be exempt from any subsequent consume collections.
        var exemptSessions = new HashSet<ICommonSession>();
        var exemptEntities = new HashSet<EntityUid>();

        foreach (var consumeCollection in communicationChannel.ConsumeCollections)
        {
            var eligibleConsumerSessions = new HashSet<ICommonSession>();

            // Adds on the EntityChatConditions list, for when you want sessions to consume messages similarly to entities.
            if (consumeCollection.UseEntitySessionConditions && consumeCollection.EntityChatConditions.Count > 0)
                consumeCollection.SessionChatConditions.Add(new ChatCondition(consumeCollection.EntityChatConditions));

            if (consumeCollection.SessionChatConditions.Count > 0)
            {
                // The list of chat conditions is made into its own chat condition here to more easily evaluate it iteratively.
                var baseChatCondition = new ChatCondition(consumeCollection.SessionChatConditions);

                var filteredConsumers = baseChatCondition.ProcessCondition(
                    targetSessions ?? _playerManager.NetworkedSessions.ToHashSet(),
                    compiledChannelParameters);

                if (filteredConsumers.Count > 0)
                    eligibleConsumerSessions = filteredConsumers;

                eligibleConsumerSessions.ExceptWith(exemptSessions);
                exemptSessions.UnionWith(eligibleConsumerSessions);
            }

            // Next, we apply any ChatModifiers from the collection.
            var consumerMessage = message;
            foreach (var chatModifier in consumeCollection.ChatModifiers)
            {
                consumerMessage = chatModifier.ProcessChatModifier(consumerMessage, compiledChannelParameters);
            }

            // Comment: I don't know whether there will ever be a ContentMarkupTag that /needs/ to be done serverside.
            // Any such behavior could just as well be done via a ChatModifier.
            // So for now we're commenting it out:

            // consumerMessage = _contentMarkupTagManager.ProcessMessage(consumerMessage);

            if (eligibleConsumerSessions.Count != 0)
            {
                // Off the message goes!
                ChatFormattedMessageToHashset(
                    consumerMessage,
                    communicationChannel,
                    eligibleConsumerSessions.Select(x => x.Channel),
                    senderEntity ?? EntityUid.Invalid,
                    communicationChannel.HideChat,
                    true //CHAT-TODO: Process properly
                );
            }

            // Send out the message to any listening entities as well.
            if (consumeCollection.EntityChatConditions.Count > 0)
            {
                var getListenerEv = new GetListenerConsumerEvent();
                _entityManager.EventBus.RaiseEvent(EventSource.Local, ref getListenerEv);

                var baseEntityChatCondition = new ChatCondition(consumeCollection.EntityChatConditions);

                var filteredEntities = baseEntityChatCondition.ProcessCondition(getListenerEv.Entities, compiledChannelParameters);

                filteredEntities.ExceptWith(exemptEntities);
                exemptEntities.UnionWith(filteredEntities);

                foreach (var consumerEntity in filteredEntities)
                {
                    var listenerConsumeEv =
                        new ListenerConsumeEvent(communicationChannel.ChatChannels, consumerMessage, compiledChannelParameters);

                    _entityManager.EventBus.RaiseLocalEvent(consumerEntity, listenerConsumeEv);
                }
            }
        }

        #endregion
    }

    #endregion

    #region Private API

    private List<CommunicationChannelPrototype> HandleChildChannels(CommunicationChannelPrototype communicationChannel, List<ProtoId<CommunicationChannelPrototype>>? childChannels)
    {
        var returnChannels = new List<CommunicationChannelPrototype>();
        if (childChannels != null)
        {
            foreach (var childChannel in childChannels)
            {
                if (_prototypeManager.TryIndex(childChannel, out var channelProto))
                {
                    //Prevents a repeatable channel from sending via another repeatable channel without a non-repeatable inbetween; should stop some looping behavior from executing.
                    if (communicationChannel.NonRepeatable || channelProto.NonRepeatable)
                    {
                        returnChannels.Add(channelProto);
                    }
                }
            }
        }

        return returnChannels;
    }

    #endregion

    #region Utility

    public void ChatFormattedMessageToHashset(FormattedMessage message,
        CommunicationChannelPrototype channel,
        IEnumerable<INetChannel> clients,
        EntityUid? source,
        bool hideChat,
        bool recordReplay,
        NetUserId? author = null)
    {
        var user = author == null ? null : EnsurePlayer(author);
        var netSource = _entityManager.GetNetEntity(source ?? default);
        user?.AddEntity(netSource);

        if (string.IsNullOrEmpty(message.ToMarkup()))
            return;

        var msg = new ChatMessage(channel, message, netSource, user?.Key, hideChat);
        _netManager.ServerSendToMany(new MsgChatMessage() { Message = msg }, clients.ToList());

        if (!recordReplay)
            return;

        if ((channel.ChatChannels & ChatChannel.AdminRelated) == 0 ||
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
            //DispatchServerMessage(player, feedback);

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
