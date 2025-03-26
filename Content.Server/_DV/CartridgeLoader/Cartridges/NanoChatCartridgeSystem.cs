using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.CartridgeLoader;
using Content.Server.Power.Components;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Server.Station.Systems;
using Content.Shared._DV.CartridgeLoader.Cartridges;
using Content.Shared._DV.NanoChat;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.Database;
using Content.Shared.PDA;
using Content.Shared.Radio.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._DV.CartridgeLoader.Cartridges;

public sealed class NanoChatCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedNanoChatSystem _nanoChat = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private EntityQuery<PdaComponent> _pdaQuery;
    private EntityQuery<NanoChatCardComponent> _cardQuery;

    // Messages in notifications get cut off after this point
    // no point in storing it on the comp
    private const int NotificationMaxLength = 64;

    // The max length of the name and job title on the notification before being truncated.
    private const int NotificationTitleMaxLength = 32;

    public override void Initialize()
    {
        base.Initialize();

        _pdaQuery = GetEntityQuery<PdaComponent>();
        _cardQuery = GetEntityQuery<NanoChatCardComponent>();

        SubscribeLocalEvent<CartridgeLoaderComponent, ActiveProgramChangedEvent>(OnActiveProgramChanged);

        SubscribeLocalEvent<CartridgeLoaderComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<CartridgeLoaderComponent, BoundUIClosedEvent>(OnUiClosed);

        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<NanoChatCartridgeComponent, CartridgeMessageEvent>(OnMessage);
    }

    private void OnActiveProgramChanged(Entity<CartridgeLoaderComponent> ent, ref ActiveProgramChangedEvent args)
    {
        if (!_pdaQuery.TryGetComponent(ent, out var pda) || pda.ContainedId is not { } cardUid)
            return;

        if (!_cardQuery.TryGetComponent(cardUid, out var nanoChatCard))
            return;

        _nanoChat.SetClosed((cardUid, nanoChatCard), !HasComp<NanoChatCartridgeComponent>(args.NewActiveProgram));
    }

    private void OnUiOpened(Entity<CartridgeLoaderComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!PdaUiKey.Key.Equals(args.UiKey))
            return;

        if (!_pdaQuery.TryGetComponent(ent, out var pda) || pda.ContainedId is not { } cardUid)
            return;

        if (!_cardQuery.TryGetComponent(cardUid, out var nanoChatCard))
            return;

        if (nanoChatCard.IsClosed)
            _nanoChat.SetClosed((cardUid, nanoChatCard), !HasComp<NanoChatCartridgeComponent>(ent.Comp.ActiveProgram));

    }

    private void OnUiClosed(Entity<CartridgeLoaderComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!PdaUiKey.Key.Equals(args.UiKey))
            return;

        if (!_pdaQuery.TryGetComponent(ent, out var pda) || pda.ContainedId is not { } cardUid)
            return;

        if (!_cardQuery.TryGetComponent(cardUid, out var nanoChatCard))
            return;

        // Since the UI got closed we always set it to be closed
        if (!nanoChatCard.IsClosed)
            _nanoChat.SetClosed((cardUid, nanoChatCard), true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Update card references for any cartridges that need it
        var query = EntityQueryEnumerator<NanoChatCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var nanoChat, out var cartridge))
        {
            if (cartridge.LoaderUid == null)
                continue;

            // Check if we need to update our card reference
            if (!_pdaQuery.TryGetComponent(cartridge.LoaderUid, out var pda))
                continue; // TODO: This is slow as fuck, should be just listening to container events after PDA refactor.
                          // There was a reason I did this in the Update loop but I can't remember.

            var newCard = pda.ContainedId;
            var currentCard = nanoChat.Card;

            // If the cards match, nothing to do
            if (newCard == currentCard)
                continue;

            // Update card reference
            nanoChat.Card = newCard;

            // Update UI state since card reference changed
            UpdateUI((uid, nanoChat), cartridge.LoaderUid.Value);
        }
    }

    /// <summary>
    ///     Handles incoming UI messages from the NanoChat cartridge.
    /// </summary>
    private void OnMessage(Entity<NanoChatCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is not NanoChatUiMessageEvent msg)
            return;

        if (!GetCardEntity(GetEntity(args.LoaderUid), out var card))
            return;

        switch (msg.Type)
        {
            case NanoChatUiMessageType.NewChat:
                HandleNewChat(card, msg);
                break;
            case NanoChatUiMessageType.SelectChat:
                HandleSelectChat(card, msg);
                break;
            case NanoChatUiMessageType.EditChat:
                HandleEditChat(card, msg);
                break;
            case NanoChatUiMessageType.CloseChat:
                HandleCloseChat(card);
                break;
            case NanoChatUiMessageType.ToggleMute:
                HandleToggleMute(card);
                break;
            case NanoChatUiMessageType.ToggleMuteChat:
                HandleToggleMuteChat(card, msg);
                break;
            case NanoChatUiMessageType.DeleteChat:
                HandleDeleteChat(card, msg);
                break;
            case NanoChatUiMessageType.SendMessage:
                HandleSendMessage(ent, card, msg);
                break;
            case NanoChatUiMessageType.ToggleListNumber:
                HandleToggleListNumber(card);
                break;
        }

        UpdateUI(ent, GetEntity(args.LoaderUid));
    }

    /// <summary>
    ///     Gets the ID card entity associated with a PDA.
    /// </summary>
    /// <param name="loaderUid">The PDA entity ID</param>
    /// <param name="card">Output parameter containing the found card entity and component</param>
    /// <returns>True if a valid NanoChat card was found</returns>
    private bool GetCardEntity(
        EntityUid loaderUid,
        out Entity<NanoChatCardComponent> card)
    {
        card = default;

        // Get the PDA and check if it has an ID card
        if (!_pdaQuery.TryGetComponent(loaderUid, out var pda) || pda.ContainedId == null)
            return false;

        if (!_cardQuery.TryGetComponent(pda.ContainedId.Value, out var idCard))
            return false;

        card = (pda.ContainedId.Value, idCard);
        return true;
    }

    /// <summary>
    ///     Handles creation of a new chat conversation.
    /// </summary>
    private void HandleNewChat(Entity<NanoChatCardComponent> card, NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber == null || msg.Content == null || msg.RecipientNumber == card.Comp.Number)
            return;

        var name = msg.Content;
        if (!string.IsNullOrWhiteSpace(name))
        {
            name = name.Trim();
            if (name.Length > IdCardConsoleComponent.MaxFullNameLength)
                name = name[..IdCardConsoleComponent.MaxFullNameLength];
        }

        var jobTitle = msg.RecipientJob;
        if (!string.IsNullOrWhiteSpace(jobTitle))
        {
            jobTitle = jobTitle.Trim();
            if (jobTitle.Length > IdCardConsoleComponent.MaxJobTitleLength)
                jobTitle = jobTitle[..IdCardConsoleComponent.MaxJobTitleLength];
        }

        // Add new recipient
        var recipient = new NanoChatRecipient(msg.RecipientNumber.Value,
            name,
            jobTitle);

        // Initialize or update recipient
        _nanoChat.SetRecipient((card, card.Comp), msg.RecipientNumber.Value, recipient);

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(msg.Actor):user} created new NanoChat conversation with #{msg.RecipientNumber:D4} ({name})");

        var recipientEv = new NanoChatRecipientUpdatedEvent(card);
        RaiseLocalEvent(ref recipientEv);
        UpdateUIForCard(card);
    }

    /// <summary>
    ///     Handles selecting a chat conversation.
    /// </summary>
    private void HandleSelectChat(Entity<NanoChatCardComponent> card, NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber == null)
            return;

        _nanoChat.SetCurrentChat((card, card.Comp), msg.RecipientNumber);

        // Clear unread flag when selecting chat
        if (_nanoChat.GetRecipient((card, card.Comp), msg.RecipientNumber.Value) is { } recipient)
        {
            _nanoChat.SetRecipient((card, card.Comp),
                msg.RecipientNumber.Value,
                recipient with { HasUnread = false });
        }
    }

    /// <summary>
    ///     Handles editing the current chat conversation.
    /// </summary>
    private void HandleEditChat(Entity<NanoChatCardComponent> card, NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber == null || msg.Content == null || msg.RecipientNumber == card.Comp.Number ||
            _nanoChat.GetRecipient((card, card.Comp), msg.RecipientNumber.Value) is not { } recipient)
            return;

        var name = msg.Content;
        if (!string.IsNullOrWhiteSpace(name))
        {
            name = name.Trim();
            if (name.Length > IdCardConsoleComponent.MaxFullNameLength)
                name = name[..IdCardConsoleComponent.MaxFullNameLength];
        }

        var jobTitle = msg.RecipientJob;
        if (!string.IsNullOrWhiteSpace(jobTitle))
        {
            jobTitle = jobTitle.Trim();
            if (jobTitle.Length > IdCardConsoleComponent.MaxJobTitleLength)
                jobTitle = jobTitle[..IdCardConsoleComponent.MaxJobTitleLength];
        }

        // Update recipient
        recipient.Name = name;
        recipient.JobTitle = jobTitle;

        _nanoChat.SetRecipient((card, card.Comp), msg.RecipientNumber.Value, recipient);

        var recipientEv = new NanoChatRecipientUpdatedEvent(card);
        RaiseLocalEvent(ref recipientEv);
        UpdateUIForCard(card);
    }

    /// <summary>
    ///     Handles closing the current chat conversation.
    /// </summary>
    private void HandleCloseChat(Entity<NanoChatCardComponent> card)
    {
        _nanoChat.SetCurrentChat((card, card.Comp), null);
    }

    /// <summary>
    ///     Handles deletion of a chat conversation.
    /// </summary>
    private void HandleDeleteChat(Entity<NanoChatCardComponent> card, NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber == null || card.Comp.Number == null)
            return;

        // Delete chat but keep the messages
        var deleted = _nanoChat.TryDeleteChat((card, card.Comp), msg.RecipientNumber.Value, true);

        if (!deleted)
            return;

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(msg.Actor):user} deleted NanoChat conversation with #{msg.RecipientNumber:D4}");

        UpdateUIForCard(card);
    }

    /// <summary>
    ///     Handles toggling notification mute state.
    /// </summary>
    private void HandleToggleMute(Entity<NanoChatCardComponent> card)
    {
        _nanoChat.SetNotificationsMuted((card, card.Comp), !_nanoChat.GetNotificationsMuted((card, card.Comp)));
        UpdateUIForCard(card);
    }

    private void HandleToggleMuteChat(Entity<NanoChatCardComponent> card, NanoChatUiMessageEvent msg)
    {
        Log.Debug($"Toggling mute for chat #{msg.RecipientNumber:D4} on card #{card.Comp.Number:D4}");
        if (msg.RecipientNumber is not uint chat)
            return;
        _nanoChat.ToggleChatMuted((card, card.Comp), chat);
        UpdateUIForCard(card);
    }

    private void HandleToggleListNumber(Entity<NanoChatCardComponent> card)
    {
        _nanoChat.SetListNumber((card, card.Comp), !_nanoChat.GetListNumber((card, card.Comp)));
        UpdateUIForAllCards();
    }

    /// <summary>
    ///     Handles sending a new message in a chat conversation.
    /// </summary>
    private void HandleSendMessage(Entity<NanoChatCartridgeComponent> cartridge,
        Entity<NanoChatCardComponent> card,
        NanoChatUiMessageEvent msg)
    {
        if (msg.RecipientNumber == null || msg.Content == null || card.Comp.Number == null)
            return;

        if (!EnsureRecipientExists(card, msg.RecipientNumber.Value))
            return;

        var content = msg.Content;
        if (!string.IsNullOrWhiteSpace(content))
        {
            content = content.Trim();
            if (content.Length > NanoChatMessage.MaxContentLength)
                content = content[..NanoChatMessage.MaxContentLength];
        }

        // Create and store message for sender
        var message = new NanoChatMessage(
            _timing.CurTime,
            content,
            (uint)card.Comp.Number
        );

        // Attempt delivery
        var (deliveryFailed, recipients) = AttemptMessageDelivery(cartridge, msg.RecipientNumber.Value);

        // Update delivery status
        message = message with { DeliveryFailed = deliveryFailed };

        // Store message in sender's outbox under recipient's number
        _nanoChat.AddMessage((card, card.Comp), msg.RecipientNumber.Value, message);

        // Log message attempt
        var recipientsText = recipients.Count > 0
            ? string.Join(", ", recipients.Select((Entity<NanoChatCardComponent> r) => ToPrettyString(r)))
            : $"#{msg.RecipientNumber:D4}";

        _adminLogger.Add(LogType.Chat,
            LogImpact.Low,
            $"{ToPrettyString(card):user} sent NanoChat message to {recipientsText}: {content}{(deliveryFailed ? " [DELIVERY FAILED]" : "")}");

        var msgEv = new NanoChatMessageReceivedEvent(card);
        RaiseLocalEvent(ref msgEv);

        if (deliveryFailed)
            return;

        foreach (var recipient in recipients)
        {
            DeliverMessageToRecipient(card, recipient, message);
        }
    }

    /// <summary>
    ///     Ensures a recipient exists in the sender's contacts.
    /// </summary>
    /// <param name="card">The card to check contacts for</param>
    /// <param name="recipientNumber">The recipient's number to check</param>
    /// <returns>True if the recipient exists or was created successfully</returns>
    private bool EnsureRecipientExists(Entity<NanoChatCardComponent> card, uint recipientNumber)
    {
        return _nanoChat.EnsureRecipientExists((card, card.Comp), recipientNumber, GetCardInfo(recipientNumber));
    }

    /// <summary>
    ///     Attempts to deliver a message to recipients.
    /// </summary>
    /// <param name="sender">The sending cartridge entity</param>
    /// <param name="recipientNumber">The recipient's number</param>
    /// <returns>Tuple containing delivery status and recipients if found.</returns>
    private (bool failed, List<Entity<NanoChatCardComponent>> recipient) AttemptMessageDelivery(
        Entity<NanoChatCartridgeComponent> sender,
        uint recipientNumber)
    {
        // First verify we can send from this device
        var channel = _prototype.Index(sender.Comp.RadioChannel);
        var sendAttemptEvent = new RadioSendAttemptEvent(channel, sender);
        RaiseLocalEvent(ref sendAttemptEvent);
        if (sendAttemptEvent.Cancelled)
            return (true, new List<Entity<NanoChatCardComponent>>());

        var foundRecipients = new List<Entity<NanoChatCardComponent>>();

        // Find all cards with matching number
        var cardQuery = EntityQueryEnumerator<NanoChatCardComponent>();
        while (cardQuery.MoveNext(out var cardUid, out var card))
        {
            if (card.Number != recipientNumber)
                continue;

            foundRecipients.Add((cardUid, card));
        }

        if (foundRecipients.Count == 0)
            return (true, foundRecipients);

        // Now check if any of these cards can receive
        var deliverableRecipients = new List<Entity<NanoChatCardComponent>>();
        foreach (var recipient in foundRecipients)
        {
            // Find any cartridges that have this card
            var cartridgeQuery = EntityQueryEnumerator<NanoChatCartridgeComponent, ActiveRadioComponent>();
            while (cartridgeQuery.MoveNext(out var receiverUid, out var receiverCart, out _))
            {
                if (receiverCart.Card != recipient.Owner)
                    continue;

                // Check if devices are on same map
                var recipientMap = Transform(receiverUid).MapID;
                var senderMap = Transform(sender).MapID;
                // Must be on the same map/station unless long-range is allowed
                if (!channel.LongRange && recipientMap != senderMap)
                {
                    break;
                }

                /* Must have an active common server
                if (HasActiveServer(senderMap))
                    continue;*/

                // Check if recipient can receive
                var receiveAttemptEv = new RadioReceiveAttemptEvent(channel, sender, receiverUid);
                RaiseLocalEvent(ref receiveAttemptEv);
                if (receiveAttemptEv.Cancelled)
                    continue;

                // Found valid cartridge that can receive
                deliverableRecipients.Add(recipient);
                break; // Only need one valid cartridge per card
            }
        }

        return (deliverableRecipients.Count == 0, deliverableRecipients);
    }

    private bool HasActiveServer(MapId mapId)
    {
        // I have no idea why this isn't public in the RadioSystem
        var query =
            EntityQueryEnumerator<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent>();

        while (query.MoveNext(out var uid, out _, out _, out var power))
        {
            if (Transform(uid).MapID == mapId && power.Powered)
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Delivers a message to the recipient and handles associated notifications.
    /// </summary>
    /// <param name="sender">The sender's card entity</param>
    /// <param name="recipient">The recipient's card entity</param>
    /// <param name="message">The <see cref="NanoChatMessage" /> to deliver</param>
    private void DeliverMessageToRecipient(Entity<NanoChatCardComponent> sender,
        Entity<NanoChatCardComponent> recipient,
        NanoChatMessage message)
    {
        if (sender.Comp.Number is not uint senderNumber)
            return;

        // Always try to get and add sender info to recipient's contacts
        if (!EnsureRecipientExists(recipient, senderNumber))
            return;

        _nanoChat.AddMessage((recipient, recipient.Comp), senderNumber, message with { DeliveryFailed = false });


        if (recipient.Comp.IsClosed || _nanoChat.GetCurrentChat((recipient, recipient.Comp)) != senderNumber)
            HandleUnreadNotification(recipient, message, senderNumber);

        var msgEv = new NanoChatMessageReceivedEvent(recipient);
        RaiseLocalEvent(ref msgEv);
        UpdateUIForCard(recipient);
    }

    /// <summary>
    ///     Handles unread message notifications and updates unread status.
    /// </summary>
    private void HandleUnreadNotification(Entity<NanoChatCardComponent> recipient,
        NanoChatMessage message,
        uint senderNumber)
    {
        // Get sender name from contacts or fall back to number
        var recipients = _nanoChat.GetRecipients((recipient, recipient.Comp));
        var senderName = recipients.TryGetValue(message.SenderId, out var senderRecipient)
            ? senderRecipient.Name
            : $"#{message.SenderId:D4}";
        var hasSelectedCurrentChat = _nanoChat.GetCurrentChat((recipient, recipient.Comp)) == senderNumber;

        // Update unread status
        if (!hasSelectedCurrentChat)
            _nanoChat.SetRecipient((recipient, recipient.Comp),
                message.SenderId,
                senderRecipient with { HasUnread = true });

        // Temporary local to avoid trouble with read-only access; Contains doesn't modify the collection
        HashSet<uint> mutedChats = recipient.Comp.MutedChats;
        if (recipient.Comp.NotificationsMuted ||
            mutedChats.Contains(message.SenderId) ||
            recipient.Comp.PdaUid is not { } pdaUid ||
            !TryComp<CartridgeLoaderComponent>(pdaUid, out var loader) ||
            // Don't notify if the recipient has the NanoChat program open with this chat selected.
            (hasSelectedCurrentChat &&
                _ui.IsUiOpen(pdaUid, PdaUiKey.Key) &&
                HasComp<NanoChatCartridgeComponent>(loader.ActiveProgram)))
            return;

        var title = "";
        if (!string.IsNullOrEmpty(senderRecipient.JobTitle))
        {
            var titleRecipient = Truncate(Loc.GetString("nano-chat-new-message-title-recipient",
                ("sender", senderName), ("jobTitle", senderRecipient.JobTitle)), NotificationTitleMaxLength, " \\[...\\]");
            title = Loc.GetString("nano-chat-new-message-title", ("sender", titleRecipient));
        }
        else
            title = Loc.GetString("nano-chat-new-message-title", ("sender", senderName));

        _cartridge.SendNotification(pdaUid,
            title,
            Loc.GetString("nano-chat-new-message-body", ("message", Truncate(message.Content, NotificationMaxLength, " [...]"))),
            loader);
    }

    /// <summary>
    ///     Updates the UI for any PDAs containing the specified card.
    /// </summary>
    private void UpdateUIForCard(EntityUid cardUid)
    {
        // Find any PDA containing this card and update its UI
        var query = EntityQueryEnumerator<NanoChatCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var cartridge))
        {
            if (comp.Card != cardUid || cartridge.LoaderUid == null)
                continue;

            UpdateUI((uid, comp), cartridge.LoaderUid.Value);
        }
    }

    /// <summary>
    ///     Updates the UI for all PDAs containing a NanoChat cartridge.
    /// </summary>
    private void UpdateUIForAllCards()
    {
        // Find any PDA containing this card and update its UI
        var query = EntityQueryEnumerator<NanoChatCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var cartridge))
        {
            if (cartridge.LoaderUid is { } loader)
                UpdateUI((uid, comp), loader);
        }
    }

    /// <summary>
    ///     Gets the <see cref="NanoChatRecipient" /> for a given NanoChat number.
    /// </summary>
    private NanoChatRecipient? GetCardInfo(uint number)
    {
        // Find card with this number to get its info
        var query = EntityQueryEnumerator<NanoChatCardComponent>();
        while (query.MoveNext(out var uid, out var card))
        {
            if (card.Number != number)
                continue;

            // Try to get job title from ID card if possible
            string? jobTitle = null;
            var name = "Unknown";
            if (TryComp<IdCardComponent>(uid, out var idCard))
            {
                jobTitle = idCard.LocalizedJobTitle;
                name = idCard.FullName ?? name;
            }

            return new NanoChatRecipient(number, name, jobTitle);
        }

        return null;
    }

    /// <summary>
    ///     Truncates a string to a maximum length.
    /// </summary>
    private static string Truncate(string text, int maxLength, string overflowText = "...") =>
        text.Length <= maxLength
            ? text
            : text[..(maxLength - overflowText.Length)] + overflowText;

    private void OnUiReady(Entity<NanoChatCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        _cartridge.RegisterBackgroundProgram(args.Loader, ent);
        UpdateUI(ent, args.Loader);
    }

    private void UpdateUI(Entity<NanoChatCartridgeComponent> ent, EntityUid loader)
    {
        List<NanoChatRecipient>? contacts;
        if (_station.GetOwningStation(loader) is { } station)
        {
            ent.Comp.Station = station;

            contacts = [];

            var query = AllEntityQuery<NanoChatCardComponent, IdCardComponent>();
            while (query.MoveNext(out var entityId, out var nanoChatCard, out var idCardComponent))
            {
                if (nanoChatCard.ListNumber && nanoChatCard.Number is uint nanoChatNumber && idCardComponent.FullName is string fullName && _station.GetOwningStation(entityId) == station)
                {
                    contacts.Add(new NanoChatRecipient(nanoChatNumber, fullName));
                }
            }
            contacts.Sort((contactA, contactB) => string.CompareOrdinal(contactA.Name, contactB.Name));
        }
        else
        {
            contacts = null;
        }

        var recipients = new Dictionary<uint, NanoChatRecipient>();
        var messages = new Dictionary<uint, List<NanoChatMessage>>();
        var mutedChats = new HashSet<uint>();
        uint? currentChat = null;
        uint ownNumber = 0;
        var maxRecipients = 50;
        var notificationsMuted = false;
        var listNumber = false;

        if (ent.Comp.Card != null && _cardQuery.TryGetComponent(ent.Comp.Card, out var card))
        {
            recipients = card.Recipients;
            messages = card.Messages;
            mutedChats = card.MutedChats;
            currentChat = card.CurrentChat;
            ownNumber = card.Number ?? 0;
            maxRecipients = card.MaxRecipients;
            notificationsMuted = card.NotificationsMuted;
            listNumber = card.ListNumber;
        }

        var state = new NanoChatUiState(recipients,
            messages,
            mutedChats,
            contacts,
            currentChat,
            ownNumber,
            maxRecipients,
            notificationsMuted,
            listNumber);
        _cartridge.UpdateCartridgeUiState(loader, state);
    }
}
