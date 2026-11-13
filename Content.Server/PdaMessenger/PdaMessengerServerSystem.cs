using System.Linq;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.GameTicking;
using Content.Server.Power.EntitySystems;
using Content.Shared.CCVar;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.PdaMessenger;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Server.PdaMessenger;

/// <summary>
///     Keeps track of which PDAs are online, relays messages between them, and stores the history.
/// </summary>
public sealed partial class PdaMessengerServerSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private ServerGameTicker _gameTicker = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private SingletonDeviceNetServerSystem _singletonServer = default!;
    [Dependency] private IGameTiming _timing = default!;

    private bool _enabled;
    private int _maxMessageLength;
    private int _historyCap;
    private int _nextMessageId;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PdaMessengerServerComponent, DeviceNetServerDisconnectedEvent>(OnDisconnected);

        Subs.CVar(_config, CCVars.PdaMessengerEnabled, value => _enabled = value, true);
        Subs.CVar(_config, CCVars.PdaMessengerMaxMessageLength, value => _maxMessageLength = value, true);
        Subs.CVar(_config, CCVars.PdaMessengerHistoryCap, value => _historyCap = value, true);
    }

    [SubscribeLocalEvent]
    private void OnRegister(Entity<PdaMessengerServerComponent> ent, ref DeviceNetworkPacketEvent<PdaMessengerRegisterPayload> args)
    {
        if (!CanHandlePackets(ent))
            return;

        var user = args.Data.User;
        ent.Comp.Directory[user.UserId] = new PdaMessengerDirectoryEntry(args.SenderAddress, user, _timing.CurTime);
        RemoveExpired(ent, args.SenderAddress, user.UserId);

        var reply = new PdaMessengerContactsPayload
        {
            Contacts = ent.Comp.Directory.Values.Select(entry => entry.Contact).ToList(),
        };
        _deviceNetwork.SendPacket(ent.Owner, args.SenderAddress, ref reply);
    }

    [SubscribeLocalEvent]
    private void OnSend(Entity<PdaMessengerServerComponent> ent, ref DeviceNetworkPacketEvent<PdaMessengerSendPayload> args)
    {
        if (!CanHandlePackets(ent))
            return;

        var data = args.Data;
        var text = data.Text.Trim();

        // The sender's details come from the directory, so a PDA can only send as the user registered at it.
        if (data.SenderId == data.RecipientId
            || !ent.Comp.Directory.TryGetValue(data.SenderId, out var sender)
            || sender.Address != args.SenderAddress
            || !ent.Comp.Directory.TryGetValue(data.RecipientId, out var recipient)
            || text.Length == 0
            || text.Length > _maxMessageLength)
        {
            return;
        }

        var conversation = GetOrCreateConversation(ent, sender.Contact, recipient.Contact);
        var message = new PdaMessengerMessage(_nextMessageId++, sender.Contact.UserId, text, _gameTicker.RoundDuration());

        conversation.Messages.Add(message);
        conversation.TrimTo(_historyCap);

        Deliver(ent, conversation, message, recipient);
        Deliver(ent, conversation, message, sender);
    }

    [SubscribeLocalEvent]
    private void OnHistoryRequest(Entity<PdaMessengerServerComponent> ent, ref DeviceNetworkPacketEvent<PdaMessengerHistoryRequestPayload> args)
    {
        if (!CanHandlePackets(ent))
            return;

        var userId = args.Data.UserId;
        if (!ent.Comp.Directory.TryGetValue(userId, out var user) || user.Address != args.SenderAddress)
            return;

        var conversations = new List<PdaMessengerConversation>();
        if (ent.Comp.UserConversations.TryGetValue(userId, out var ids))
        {
            foreach (var id in ids)
            {
                if (ent.Comp.Conversations.TryGetValue(id, out var conversation))
                    conversations.Add(conversation);
            }
        }

        var reply = new PdaMessengerHistoryPayload { UserId = userId, Conversations = conversations };
        _deviceNetwork.SendPacket(ent.Owner, args.SenderAddress, ref reply);
    }

    /// <summary>
    ///     PDAs register with the new active server, so this one's directory is out of date.
    /// </summary>
    private void OnDisconnected(Entity<PdaMessengerServerComponent> ent, ref DeviceNetServerDisconnectedEvent args)
    {
        ent.Comp.Directory.Clear();
    }

    private void Deliver(
        Entity<PdaMessengerServerComponent> ent,
        PdaMessengerConversation conversation,
        PdaMessengerMessage message,
        PdaMessengerDirectoryEntry recipient)
    {
        var payload = new PdaMessengerDeliverPayload
        {
            RecipientId = recipient.Contact.UserId,
            ConversationId = conversation.Id,
            Participants = conversation.Participants,
            Message = message,
        };
        _deviceNetwork.SendPacket(ent.Owner, recipient.Address, ref payload);
    }

    private static PdaMessengerConversation GetOrCreateConversation(
        Entity<PdaMessengerServerComponent> ent,
        PdaMessengerContact a,
        PdaMessengerContact b)
    {
        var id = PdaMessengerConversation.GetId(a.UserId, b.UserId);
        if (!ent.Comp.Conversations.TryGetValue(id, out var conversation))
        {
            conversation = new PdaMessengerConversation { Id = id };
            ent.Comp.Conversations[id] = conversation;
        }

        // Refreshed every time, so renamed ID cards show their new name.
        conversation.Participants = new List<PdaMessengerContact> { a, b };

        foreach (var userId in new[] { a.UserId, b.UserId })
        {
            if (!ent.Comp.UserConversations.TryGetValue(userId, out var ids))
                ent.Comp.UserConversations[userId] = ids = new HashSet<string>();

            ids.Add(id);
        }

        return conversation;
    }

    /// <summary>
    ///     Drops users whose PDA stopped registering, and any other user registered at this PDA,
    ///     since a PDA holds one ID card at a time.
    /// </summary>
    private void RemoveExpired(Entity<PdaMessengerServerComponent> ent, string address, string currentUserId)
    {
        var cutoff = _timing.CurTime - ent.Comp.RegistrationTimeout;
        foreach (var (userId, entry) in ent.Comp.Directory)
        {
            if (entry.LastSeen < cutoff || (entry.Address == address && userId != currentUserId))
                ent.Comp.Directory.Remove(userId);
        }
    }

    /// <summary>
    ///     A server that never had power gets no PowerChangedEvent, so SingletonDeviceNetServer
    ///     can still treat it as active. Checking power here keeps it from answering.
    /// </summary>
    private bool CanHandlePackets(Entity<PdaMessengerServerComponent> ent)
    {
        return _enabled && _singletonServer.IsActiveServer(ent) && _power.IsPowered(ent);
    }
}
