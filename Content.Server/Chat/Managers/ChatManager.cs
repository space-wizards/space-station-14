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
        // CHAT-TODO: Get this (maybe move to ChatSystem?)
        //DispatchServerAnnouncement(Loc.GetString(val ? "chat-manager-ooc-chat-enabled-message" : "chat-manager-ooc-chat-disabled-message"));
    }

    private void OnAdminOocEnabledChanged(bool val)
    {
        if (_adminOocEnabled == val) return;

        _adminOocEnabled = val;
        // CHAT-TODO: Get this (maybe move to ChatSystem?)
        //DispatchServerAnnouncement(Loc.GetString(val ? "chat-manager-admin-ooc-chat-enabled-message" : "chat-manager-admin-ooc-chat-disabled-message"));
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

    #endregion

    #region Public OOC Chat API

    #endregion

    #region Private API

    #endregion

    #region Main Content

    public void HandleMessage(string message, string communicationChannel, ICommonSession? senderSession, EntityUid? senderEntity, HashSet<ICommonSession>? targetSessions = null, bool escapeText = true, Dictionary<Enum, object>? supplierParameters = null, bool logMessage = true)
    {
        var formattedMessage = escapeText ? FormattedMessage.FromMarkupPermissive(message, out string? error) : FormattedMessage.FromUnformatted(FormattedMessage.EscapeText(message));

        HandleMessage(formattedMessage, communicationChannel, senderSession, senderEntity, targetSessions, supplierParameters);
    }

    public void HandleMessage(FormattedMessage message, string communicationChannel, ICommonSession? senderSession, EntityUid? senderEntity, HashSet<ICommonSession>? targetSessions = null, Dictionary<Enum, object>? supplierParameters = null, bool logMessage = true)
    {
        _prototypeManager.TryIndex<CommunicationChannelPrototype>(communicationChannel, out var proto);
        var usedCommsTypes = new List<CommunicationChannelPrototype>();

        if (proto != null)
            HandleMessage(message, proto, senderSession, senderEntity, ref usedCommsTypes, targetSessions, supplierParameters);
    }

    public void HandleMessage(FormattedMessage message, string communicationChannel, ICommonSession? senderSession, EntityUid? senderEntity, ref List<CommunicationChannelPrototype> usedCommsTypes, HashSet<ICommonSession>? targetSessions = null, Dictionary<Enum, object>? supplierParameters = null, bool logMessage = true)
    {
        _prototypeManager.TryIndex<CommunicationChannelPrototype>(communicationChannel, out var proto);

        if (proto != null)
            HandleMessage(message, proto, senderSession, senderEntity, ref usedCommsTypes, targetSessions, supplierParameters);
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
    public void HandleMessage(
        FormattedMessage message,
        CommunicationChannelPrototype communicationChannel,
        ICommonSession? senderSession,
        EntityUid? senderEntity,
        ref List<CommunicationChannelPrototype> usedCommsChannels,
        HashSet<ICommonSession>? targetSessions = null,
        Dictionary<Enum, object>? supplierParameters = null,
        bool logMessage = true)
    {
        //CHAT-TODO: Step 1
        //First there needs to be verification; can the client/entity attempting to send this message actually do it?

        //If the comms type is non-repeatable, i.e. the message may only ever be sent once on that comms channel, check for it and block if it has been sent.
        if (communicationChannel.NonRepeatable && usedCommsChannels.Contains(communicationChannel))
            return;

        // If senderSession is null, it means the server is sending the message.
        if (senderSession != null)
        {
            var publishSessionChatConditions = communicationChannel.PublishSessionChatConditions;

            var allowPublish = false;
            foreach (var condition in publishSessionChatConditions)
            {
                var result = condition.ProcessCondition(new HashSet<ICommonSession>() { senderSession }, senderEntity);

                //If the session succeeds in any of the publish conditions, no need to do any remaining ones.
                if (result.Count > 0)
                {
                    allowPublish = true;
                    break;
                }
            }

            if (!allowPublish)
            {
                return;
            }
            Logger.Debug(senderSession.ToString()!);
        }
        else if (!communicationChannel.AllowServerMessages)
        {
            return;
        }

        //CHAT-TODO: Step 1b
        // If senderEntity is null, it means it's either a player in a lobby messaging, or the server sending a message without any associated entity.
        if (senderEntity != null)
        {
            var basePublishEntityChatCondition = new EntityChatCondition(communicationChannel.PublishEntityChatConditions);

            var result = basePublishEntityChatCondition.ProcessCondition(new HashSet<EntityUid>() { senderEntity.Value }, senderEntity);

            if (result.Count == 0)
                return;

            foreach (var test in result)
            {
                if (test != null)
                    Logger.Debug(test.ToString()!);
            }
        }
        else if (!communicationChannel.AllowEntitylessMessages)
        {
            Logger.Debug("EntitylessMessageNotAllowed");
            return;
        }

        //At this point we may assume that the message is publishing; as such, it should be recorded in the usedCommsTypes.
        if (communicationChannel.NonRepeatable)
            usedCommsChannels.Add(communicationChannel);

        //CHAT-TODO: Step 2
        //Then, there needs to be a filter for who the message should be sent to. This value gets saved.

        var exemptSessions = new HashSet<ICommonSession>();
        var exemptEntities = new HashSet<EntityUid>();

        foreach (var consumeCollection in communicationChannel.ConsumeCollections)
        {

            // First we look at eligible sessions
            var eligibleConsumerSessions = new HashSet<ICommonSession>();
            if (consumeCollection.ConsumeSessionChatConditions.Count > 0)
            {
                foreach (var condition in consumeCollection.ConsumeSessionChatConditions)
                {
                    eligibleConsumerSessions.UnionWith(
                        condition.ProcessCondition(targetSessions ?? _playerManager.NetworkedSessions.ToHashSet(), senderEntity));
                }

                eligibleConsumerSessions.ExceptWith(exemptSessions);
                exemptSessions.UnionWith(eligibleConsumerSessions);

                // CHAT-TODO: Remove debug:
                foreach (var test in eligibleConsumerSessions)
                {
                    if (test != null)
                        Logger.Debug(test.ToString()!);
                }
            }

            //CHAT-TODO: Step 2b
            // Then, we look at eligible entities

            var eligibleConsumerEntities = new HashSet<EntityUid>();
            if (consumeCollection.ConsumeEntityChatConditions.Count > 0)
            {
                // First we get all any entity with a ListenerComponent attached, to not have to iterate over every single entities.
                var ev = new GetListenerConsumerEvent();
                _entityManager.EventBus.RaiseEvent(EventSource.Local, ref ev);

                // The list of chat conditions is made into its own chat condition, to more easily evaluate it iteratively.
                var baseConsumerChatCondition =
                    new EntityChatCondition(consumeCollection.ConsumeEntityChatConditions);

                var filteredConsumers = baseConsumerChatCondition.ProcessCondition(ev.Entities, senderEntity);

                if (filteredConsumers.Count > 0)
                    eligibleConsumerEntities = filteredConsumers;

                eligibleConsumerEntities.ExceptWith(exemptEntities);
                exemptEntities.UnionWith(eligibleConsumerEntities);
            }

            //CHAT-TODO: Step 3
            //Then, then it also gets passed on to any other communication types (e.g. radio + whisper)

            if (communicationChannel.ChildCommunicationChannels != null)
            {
                foreach (var childChannel in communicationChannel.ChildCommunicationChannels)
                {
                    if (_prototypeManager.TryIndex(childChannel, out var channelProto))
                    {
                        if (communicationChannel.NonRepeatable || channelProto.NonRepeatable)
                        {
                            //Prevents a repeatable channel from sending via another repeatable channel without a non-repeatable inbetween; should stop some looping behavior from executing.
                            HandleMessage(
                                message,
                                childChannel,
                                senderSession,
                                senderEntity,
                                ref usedCommsChannels,
                                targetSessions);
                        }
                    }
                }
            }

            //CHAT-TODO: Step 3.5
            //Supply markup tags.

            var consumerMessage = message;

            foreach (var markupSupplier in consumeCollection.CollectionMarkupNodes)
            {
                //CHAT-TODO: message needs to be converted to FormattedMessage
                consumerMessage = markupSupplier.ProcessNodeSupplier(consumerMessage, supplierParameters);
            }

            //CHAT-TODO: Step 4
            //Then, it applies the serversideMessageMutators. This should NOT be formatting and rarely text changes, unless strictly necessary (animal speak/whisper censorship)
            Logger.Debug("Pre-step4: " + consumerMessage);
            consumerMessage = ContentMarkupTagManager.ProcessMessage(consumerMessage);
            Logger.Debug("Post-step4: " + consumerMessage);

            ChatFormattedMessageToHashset(
                consumerMessage,
                communicationChannel,
                eligibleConsumerSessions.Select(x => x.Channel),
                senderEntity ?? EntityUid.Invalid,
                false, //CHAT-TODO: Process properly
                true //CHAT-TODO: Process properly
                );

            //CHAT-TODO: Step5b
            //Also gotta send it out to all consuming entities

            foreach (var entity in eligibleConsumerEntities)
            {
                var ev = new ListenerConsumeEvent(communicationChannel.ChatChannels,
                    new FormattedMessage());

                _entityManager.EventBus.RaiseLocalEvent(entity, ev);
            }
        }
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

        var msg = new ChatMessage(channel, message, netSource, user?.Key, hideChat);
        _netManager.ServerSendToMany(new MsgChatMessage() { Message = msg }, clients.ToList());

        if (!recordReplay)
            return;

        //CHAT-TODO: Figure out how to do this
        /*if ((channel & ChatChannel.AdminRelated) == 0 ||
            _configurationManager.GetCVar(CCVars.ReplayRecordAdminChat))
        {*/
            _replay.RecordServerMessage(msg);
        //}
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
