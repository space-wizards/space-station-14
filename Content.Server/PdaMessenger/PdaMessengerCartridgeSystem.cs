using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.GameTicking;
using Content.Shared.Paper;
using Content.Shared.PDA;
using Content.Shared.PdaMessenger;
using Content.Shared.Popups;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.PdaMessenger;

/// <summary>
///     The PDA side of the messenger: registers with the station's server, sends messages,
///     and keeps a local copy of each ID card's conversations.
/// </summary>
public sealed partial class PdaMessengerCartridgeSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SingletonDeviceNetServerSystem _singletonServer = default!;
    [Dependency] private ServerStationSystem _station = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly ProtoId<DeviceFrequencyPrototype> ServerFrequency = "PdaMessengerServer";

    private static readonly LocId MutedMessage = "pda-messenger-muted";

    private readonly HashSet<string> _usedIds = new();

    /// <summary>
    ///     Players an admin blocked from sending, for the rest of the round.
    /// </summary>
    private readonly HashSet<NetUserId> _mutedPlayers = new();

    private bool _enabled;
    private int _historyCap;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        Subs.CVar(_config, CCVars.PdaMessengerEnabled, value => _enabled = value, true);
        Subs.CVar(_config, CCVars.PdaMessengerHistoryCap, value => _historyCap = value, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PdaMessengerCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var messenger, out var cartridge))
        {
            if (cartridge.LoaderUid is not { } loader || _timing.CurTime < messenger.NextRegister)
                continue;

            messenger.NextRegister = _timing.CurTime + messenger.RegisterInterval;
            Register((uid, messenger), loader);
        }
    }

    /// <summary>
    ///     Blocks or unblocks a player from sending messages, for the rest of the round.
    /// </summary>
    /// <returns>False if the player was already muted or unmuted.</returns>
    public bool SetMuted(NetUserId player, bool muted)
    {
        return muted ? _mutedPlayers.Add(player) : _mutedPlayers.Remove(player);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _usedIds.Clear();
        _mutedPlayers.Clear();
    }

    [SubscribeLocalEvent]
    private void OnIdMapInit(Entity<PdaMessengerIdComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.UserId != null)
        {
            _usedIds.Add(ent.Comp.UserId);
            return;
        }

        string userId;
        do
        {
            userId = $"{_random.Next(0x10000):X4}-{_random.Next(0x10000):X4}";
        } while (!_usedIds.Add(userId));

        ent.Comp.UserId = userId;
    }

    [SubscribeLocalEvent]
    private void OnIdInserted(Entity<PdaMessengerIdComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        RegisterNextTick(args.Container.Owner);
    }

    [SubscribeLocalEvent]
    private void OnIdRemoved(Entity<PdaMessengerIdComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        RegisterNextTick(args.Container.Owner);
    }

    /// <summary>
    ///     An ID card went into or out of a PDA: register on the next tick, so the UI switches user right away.
    ///     Not during the insertion itself, which also happens while starting gear spawns.
    /// </summary>
    private void RegisterNextTick(EntityUid loader)
    {
        if (!HasComp<CartridgeLoaderComponent>(loader))
            return;

        foreach (var messenger in GetMessengers(loader))
        {
            messenger.Comp.NextRegister = TimeSpan.Zero;
        }
    }

    [SubscribeLocalEvent]
    private void OnUiReady(Entity<PdaMessengerCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUi(ent, args.Loader);
    }

    [SubscribeLocalEvent]
    private void OnUiMessage(Entity<PdaMessengerCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not PdaMessengerUiMessageEvent message)
            return;

        var loader = GetEntity(args.LoaderUid);
        switch (message.Payload)
        {
            case PdaMessengerSendMessage send:
                Send(ent, loader, args.Actor, send);
                break;
            case PdaMessengerOpenConversation open:
                ent.Comp.OpenConversation = open.ConversationId;
                if (open.ConversationId != null)
                    ent.Comp.Unread.Remove(open.ConversationId);
                break;
        }

        UpdateUi(ent, loader);
    }

    // Packets from the server arrive at the PDA, not at the program, so these are subscribed on the loader.

    [SubscribeLocalEvent]
    private void OnContacts(Entity<CartridgeLoaderComponent> loader, ref DeviceNetworkPacketEvent<PdaMessengerContactsPayload> args)
    {
        if (!HasComp<PdaMessengerServerComponent>(args.Sender))
            return;

        foreach (var messenger in GetMessengers(loader))
        {
            messenger.Comp.Server = args.SenderAddress;
            var userId = messenger.Comp.User?.UserId;
            messenger.Comp.Contacts = args.Data.Contacts.Where(contact => contact.UserId != userId).ToList();
        }
    }

    [SubscribeLocalEvent]
    private void OnDeliver(Entity<CartridgeLoaderComponent> loader, ref DeviceNetworkPacketEvent<PdaMessengerDeliverPayload> args)
    {
        var data = args.Data;
        foreach (var messenger in GetMessengers(loader))
        {
            if (messenger.Comp.Server != args.SenderAddress || messenger.Comp.User?.UserId != data.RecipientId)
                continue;

            var conversation = GetOrCreateConversation(messenger, data.ConversationId);
            conversation.Participants = new List<PdaMessengerContact>(data.Participants);
            AddMessages(conversation, [data.Message]);

            if (data.Message.SenderId != data.RecipientId)
            {
                var reading = messenger.Comp.OpenConversation == conversation.Id
                              && loader.Comp.ActiveProgram == messenger.Owner;
                if (!reading)
                {
                    // Never more than the messages kept, since the oldest are dropped at the history cap.
                    var unread = messenger.Comp.Unread.GetValueOrDefault(conversation.Id) + 1;
                    messenger.Comp.Unread[conversation.Id] = Math.Min(unread, conversation.Messages.Count);
                }

                // The PDA escapes the message text, but not the header, and ID card names are player-written.
                var sender = data.Participants.FirstOrNull(contact => contact.UserId == data.Message.SenderId);
                var senderName = FormattedMessage.EscapeText(sender?.Name ?? Loc.GetString("pda-messenger-unknown-name"));
                _cartridgeLoader.SendNotification(loader,
                    Loc.GetString("pda-messenger-notification-header", ("sender", senderName)),
                    data.Message.Text);
            }

            UpdateUi(messenger, loader);
        }
    }

    [SubscribeLocalEvent]
    private void OnHistory(Entity<CartridgeLoaderComponent> loader, ref DeviceNetworkPacketEvent<PdaMessengerHistoryPayload> args)
    {
        var data = args.Data;
        foreach (var messenger in GetMessengers(loader))
        {
            if (messenger.Comp.Server != args.SenderAddress || messenger.Comp.User?.UserId != data.UserId)
                continue;

            // Merged, never replaced, so a new server with no history doesn't wipe the PDA's copy.
            foreach (var downloaded in data.Conversations)
            {
                var conversation = GetOrCreateConversation(messenger, downloaded.Id);
                conversation.Participants = new List<PdaMessengerContact>(downloaded.Participants);
                AddMessages(conversation, downloaded.Messages);
            }

            UpdateUi(messenger, loader);
        }
    }

    private void Register(Entity<PdaMessengerCartridgeComponent> ent, EntityUid loader)
    {
        var user = GetUser(loader);
        var previousUser = ent.Comp.User;
        var previousServer = ent.Comp.Server;
        var previousContacts = ent.Comp.Contacts;
        var newCard = user?.UserId != previousUser?.UserId;

        if (newCard)
            ent.Comp.OpenConversation = null;

        ent.Comp.User = user;
        ent.Comp.Server = null;

        // Delivery is synchronous: the server's Contacts reply sets Server before SendPacket returns.
        if (_enabled && user is { } self && TryGetServer(loader, out var server))
        {
            var payload = new PdaMessengerRegisterPayload { User = self };
            _deviceNetwork.SendPacket(loader, server, ref payload, _prototype.Index(ServerFrequency).Frequency);
        }

        if (ent.Comp.Server == null)
            ent.Comp.Contacts = new List<PdaMessengerContact>();
        else if ((newCard || ent.Comp.Server != previousServer) && user is { } current)
        {
            var request = new PdaMessengerHistoryRequestPayload { UserId = current.UserId };
            _deviceNetwork.SendPacket(loader, ent.Comp.Server, ref request, _prototype.Index(ServerFrequency).Frequency);
        }

        // Only resend the UI when something it shows changed. New messages update it when they arrive.
        if (user != previousUser
            || ent.Comp.Server != previousServer
            || !ent.Comp.Contacts.SequenceEqual(previousContacts)
            || GetHolderSendBlock(loader) != ent.Comp.SendBlocked)
        {
            UpdateUi(ent, loader);
        }
    }

    private void Send(
        Entity<PdaMessengerCartridgeComponent> ent,
        EntityUid loader,
        EntityUid actor,
        PdaMessengerSendMessage send)
    {
        if (ent.Comp.Server is not { } server || GetUser(loader) is not { } self || self.UserId != ent.Comp.User?.UserId)
            return;

        var text = send.Text.Trim();
        if (text.Length == 0 || GetSendBlock(actor) != null)
            return;

        // Shown whether or not the message gets through: bystanders see the typing, not the result.
        _popup.PopupEntity(Loc.GetString("pda-messenger-typing"), actor, PopupType.Small);

        var conversationId = PdaMessengerConversation.GetId(self.UserId, send.RecipientId);
        var newestBefore = TryGetConversation(ent, conversationId, out var existing) && existing.Messages.Count > 0
            ? existing.Messages[^1].Id
            : -1;

        var payload = new PdaMessengerSendPayload { SenderId = self.UserId, RecipientId = send.RecipientId, Text = text };
        _deviceNetwork.SendPacket(loader, server, ref payload, _prototype.Index(ServerFrequency).Frequency);

        // The server's Deliver arrives before SendPacket returns, so no newer message from us means it wasn't sent.
        if (!TryGetConversation(ent, conversationId, out var conversation)
            || !conversation.Messages.Any(message => message.Id > newestBefore && message.SenderId == self.UserId))
        {
            _popup.PopupEntity(Loc.GetString("pda-messenger-not-delivered"), loader, actor);
            return;
        }

        var recipient = conversation.Participants.FirstOrNull(contact => contact.UserId == send.RecipientId);
        _adminLog.Add(LogType.PdaInteract, LogImpact.Low,
            $"{ToPrettyString(actor):player} sent a PDA message as {self.Name} ({self.JobTitle}) "
            + $"to {recipient?.Name} ({recipient?.JobTitle}): {text}");
    }

    private void UpdateUi(Entity<PdaMessengerCartridgeComponent> ent, EntityUid loader)
    {
        if (!TryComp<CartridgeLoaderComponent>(loader, out var loaderComp) || loaderComp.ActiveProgram != ent.Owner)
            return;

        var user = ent.Comp.User;
        var status = !_enabled ? PdaMessengerStatus.Disabled
            : user == null ? PdaMessengerStatus.NoId
            : ent.Comp.Server == null ? PdaMessengerStatus.NoServer
            : PdaMessengerStatus.Connected;

        var conversations = user is { } self && ent.Comp.History.TryGetValue(self.UserId, out var history)
            ? history.Values.OrderByDescending(c => c.Messages.LastOrDefault().Id).ToList()
            : new List<PdaMessengerConversation>();

        ent.Comp.SendBlocked = GetHolderSendBlock(loader);

        var unread = new Dictionary<string, int>(ent.Comp.Unread);
        var state = new PdaMessengerUiState(status, user, ent.Comp.Contacts, conversations, unread, ent.Comp.SendBlocked);
        _cartridgeLoader.UpdateCartridgeUiState(loader, state, loaderComp);
    }

    /// <summary>
    ///     Gets why whoever holds the PDA can't send, for the UI. Sending itself checks the actual user.
    /// </summary>
    private LocId? GetHolderSendBlock(EntityUid loader)
    {
        return _container.TryGetContainingContainer(loader, out var container) ? GetSendBlock(container.Owner) : null;
    }

    /// <summary>
    ///     Gets why a user can't send messages, or null if they can. Anyone who can't write on paper,
    ///     like a mime keeping their vow, can't send either.
    /// </summary>
    private LocId? GetSendBlock(EntityUid user)
    {
        if (TryComp<ActorComponent>(user, out var actor) && _mutedPlayers.Contains(actor.PlayerSession.UserId))
            return MutedMessage;

        return TryComp<BlockWritingComponent>(user, out var blockWriting) ? (LocId?) blockWriting.FailWriteMessage : null;
    }

    /// <summary>
    ///     Gets the inserted ID card's user ID, name and job, or null if there's no card with a user ID.
    /// </summary>
    private PdaMessengerContact? GetUser(EntityUid loader)
    {
        if (!TryComp<PdaComponent>(loader, out var pda)
            || pda.ContainedId is not { } id
            || !TryComp<IdCardComponent>(id, out var card)
            || !TryComp<PdaMessengerIdComponent>(id, out var messengerId)
            || messengerId.UserId == null)
        {
            return null;
        }

        var name = string.IsNullOrWhiteSpace(card.FullName) ? Loc.GetString("pda-messenger-unknown-name") : card.FullName;
        return new PdaMessengerContact(messengerId.UserId, name, card.LocalizedJobTitle ?? string.Empty);
    }

    private bool TryGetServer(EntityUid loader, [NotNullWhen(true)] out string? address)
    {
        address = null;
        return _station.GetOwningStation(loader) is { } station
               && _singletonServer.TryGetActiveServerAddress<PdaMessengerServerComponent>(station, out address);
    }

    private IEnumerable<Entity<PdaMessengerCartridgeComponent>> GetMessengers(EntityUid loader)
    {
        foreach (var program in _cartridgeLoader.GetAllPrograms(loader))
        {
            if (TryComp<PdaMessengerCartridgeComponent>(program, out var messenger))
                yield return (program, messenger);
        }
    }

    private static bool TryGetConversation(
        Entity<PdaMessengerCartridgeComponent> ent,
        string? conversationId,
        [NotNullWhen(true)] out PdaMessengerConversation? conversation)
    {
        conversation = null;
        return conversationId != null
               && ent.Comp.User is { } user
               && ent.Comp.History.TryGetValue(user.UserId, out var history)
               && history.TryGetValue(conversationId, out conversation);
    }

    private static PdaMessengerConversation GetOrCreateConversation(
        Entity<PdaMessengerCartridgeComponent> ent,
        string conversationId)
    {
        var userId = ent.Comp.User!.Value.UserId;
        if (!ent.Comp.History.TryGetValue(userId, out var history))
            ent.Comp.History[userId] = history = new Dictionary<string, PdaMessengerConversation>();

        if (!history.TryGetValue(conversationId, out var conversation))
            history[conversationId] = conversation = new PdaMessengerConversation { Id = conversationId };

        return conversation;
    }

    /// <summary>
    ///     Adds messages the PDA doesn't have yet, in order, and trims to the history cap.
    /// </summary>
    private void AddMessages(PdaMessengerConversation conversation, IEnumerable<PdaMessengerMessage> messages)
    {
        var known = conversation.Messages.Select(message => message.Id).ToHashSet();
        conversation.Messages.AddRange(messages.Where(message => known.Add(message.Id)));
        conversation.Messages.Sort((a, b) => a.Id.CompareTo(b.Id));
        conversation.TrimTo(_historyCap);
    }
}
