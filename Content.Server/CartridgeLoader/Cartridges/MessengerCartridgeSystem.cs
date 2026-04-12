using Content.Server.Administration.Logs;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Database;
using Content.Shared.PDA;
using Robust.Shared.Timing;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class MessengerCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessengerCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<MessengerCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    private void OnUiReady(EntityUid uid, MessengerCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader, component);
    }

    private void OnUiMessage(EntityUid uid, MessengerCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args is not MessengerUiMessageEvent message)
            return;

        switch (message.Action)
        {
            case MessengerUiAction.OpenChat:
                if (message.RecipientName != null)
                {
                    component.ActiveChat = message.RecipientName;
                    component.UnreadContacts.Remove(message.RecipientName);
                }
                break;

            case MessengerUiAction.Back:
                component.ActiveChat = null;
                break;

            case MessengerUiAction.Send:
                if (message.RecipientName == null || message.MessageContent == null)
                    return;

                var content = message.MessageContent;
                if (content.Length > component.MaxMessageLength)
                    content = content[..component.MaxMessageLength];

                if (string.IsNullOrWhiteSpace(content))
                    return;

                var senderName = GetOwnerName(uid);
                if (senderName == null)
                    return;

                var timestamp = _timing.CurTime;
                var stored = new MessengerStoredMessage(senderName, content, timestamp);

                AddMessage(component, message.RecipientName, stored);
                DeliverToRecipient(uid, senderName, message.RecipientName, content, timestamp);

                _adminLogger.Add(LogType.PdaInteract, LogImpact.Low,
                    $"{ToPrettyString(args.Actor)} sent PDA message from '{senderName}' to '{message.RecipientName}': '{content}'");
                break;
        }

        UpdateUiState(uid, GetEntity(args.LoaderUid), component);
    }

    private void DeliverToRecipient(EntityUid senderCartridge, string senderName, string recipientName, string content, TimeSpan timestamp)
    {
        var query = EntityQueryEnumerator<MessengerCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var recipientUid, out var messenger, out var cartridge))
        {
            if (recipientUid == senderCartridge)
                continue;

            if (cartridge.LoaderUid == null)
                continue;

            var ownerName = GetOwnerName(recipientUid);
            if (ownerName == null || ownerName != recipientName)
                continue;

            var stored = new MessengerStoredMessage(senderName, content, timestamp);
            AddMessage(messenger, senderName, stored);

            if (messenger.ActiveChat != senderName)
                messenger.UnreadContacts.Add(senderName);

            _cartridgeLoader.SendNotification(
                cartridge.LoaderUid.Value,
                Loc.GetString("messenger-notification-header", ("sender", senderName)),
                content);

            UpdateUiState(recipientUid, cartridge.LoaderUid.Value, messenger);
        }
    }

    private void AddMessage(MessengerCartridgeComponent component, string contactName, MessengerStoredMessage message)
    {
        if (!component.Messages.TryGetValue(contactName, out var messages))
        {
            messages = new List<MessengerStoredMessage>();
            component.Messages[contactName] = messages;
        }

        messages.Add(message);

        while (messages.Count > component.MaxMessages)
            messages.RemoveAt(0);
    }

    private string? GetOwnerName(EntityUid cartridgeUid)
    {
        if (!TryComp<CartridgeComponent>(cartridgeUid, out var cartridge) || cartridge.LoaderUid == null)
            return null;

        if (!TryComp<PdaComponent>(cartridge.LoaderUid.Value, out var pda))
            return null;

        return pda.OwnerName;
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, MessengerCartridgeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var contacts = BuildContactList(uid);
        var currentOwner = GetOwnerName(uid) ?? "";

        var activeMessages = new List<MessengerMessageData>();
        if (component.ActiveChat != null &&
            component.Messages.TryGetValue(component.ActiveChat, out var msgs))
        {
            foreach (var msg in msgs)
                activeMessages.Add(new MessengerMessageData(msg.Sender, msg.Content, msg.Timestamp));
        }

        var state = new MessengerUiState(contacts, component.ActiveChat, activeMessages, currentOwner);
        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
    }

    private List<MessengerContact> BuildContactList(EntityUid selfCartridgeUid)
    {
        var contacts = new List<MessengerContact>();
        var selfName = GetOwnerName(selfCartridgeUid);

        MessengerCartridgeComponent? selfMessenger = null;
        Resolve(selfCartridgeUid, ref selfMessenger);

        var query = EntityQueryEnumerator<PdaComponent>();
        var seen = new HashSet<string>();

        while (query.MoveNext(out var pdaUid, out var pda))
        {
            if (pda.OwnerName == null || pda.OwnerName == selfName)
                continue;

            if (!seen.Add(pda.OwnerName))
                continue;

            if (!HasMessengerCartridge(pdaUid))
                continue;

            string? jobTitle = null;
            if (pda.ContainedId != null && TryComp<IdCardComponent>(pda.ContainedId.Value, out var id))
                jobTitle = id.LocalizedJobTitle;

            var hasUnread = selfMessenger?.UnreadContacts.Contains(pda.OwnerName) ?? false;
            contacts.Add(new MessengerContact(pda.OwnerName, jobTitle, hasUnread));
        }

        contacts.Sort((a, b) =>
        {
            if (a.HasUnread != b.HasUnread)
                return a.HasUnread ? -1 : 1;
            return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        });

        return contacts;
    }

    private bool HasMessengerCartridge(EntityUid pdaUid)
    {
        if (!TryComp<CartridgeLoaderComponent>(pdaUid, out var loader))
            return false;

        foreach (var program in _cartridgeLoader.GetAvailablePrograms(pdaUid, loader))
        {
            if (HasComp<MessengerCartridgeComponent>(GetEntity(program)))
                return true;
        }

        return false;
    }
}