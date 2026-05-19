using System.Linq;
using Content.Shared.PDA;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Radio.Components;
using Content.Server.Power.Components;
using Content.Server.GameTicking;
using Robust.Shared.Containers;
using Robust.Shared.Localization;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class MessagerCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    [Dependency] private GameTicker _gameTicker = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessagerCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<MessagerCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
    }

    /// <summary>
    /// Syncing client and server
    /// </summary>
    private void SyncUsers()
    {
        // Excluding users for later deletion
        var activeUserIdsByServer = new Dictionary<EntityUid, HashSet<int>>();

        var cartridgeQuery = EntityQueryEnumerator<MessagerCartridgeComponent>();
        while (cartridgeQuery.MoveNext(out var cartridgeUid, out _))
        {
            var userData = GetUserData(cartridgeUid);
            if (userData == null)
                continue;

            var server = GetServerForCartridge(cartridgeUid);
            if (server == null)
                continue;

            activeUserIdsByServer.TryAdd(server.Value.Uid, new HashSet<int>());
            activeUserIdsByServer[server.Value.Uid].Add(userData.Value.Id);

            SendUserData(cartridgeUid);
        }

        // Delete users
        foreach (var (serverUid, activeIds) in activeUserIdsByServer)
        {
            if (!TryComp(serverUid, out MessagerServerComponent? serverComponent))
                continue;

            var usersToRemove = serverComponent.Users.Keys
                .Where(id => !activeIds.Contains(id))
                .ToList();

            foreach (var id in usersToRemove)
            {
                serverComponent.Users.Remove(id);
            }

            if (usersToRemove.Count > 0)
            {
                Dirty(serverUid, serverComponent);
            }
        }
    }

    /// <summary>
    /// Find the Server
    /// </summary>
    private (EntityUid Uid, MessagerServerComponent Component)? GetServerForCartridge(EntityUid cartridgeUid)
    {
        if (!TryComp(cartridgeUid, out TransformComponent? transform))
            return null;

        var serverQuery = EntityQueryEnumerator<MessagerServerComponent, TransformComponent, ApcPowerReceiverComponent>();
        while (serverQuery.MoveNext(out var serverUid, out var serverComponent, out var serverTransform, out var power))
        {
            if (serverTransform.MapID != transform.MapID)
                continue;

            if (power.Powered)
                return (serverUid, serverComponent);
        }
        return null;
    }

    /// <summary>
    /// Sending user data to UserList on Server
    /// </summary>
    private void SendUserData(EntityUid cartridgeUid)
    {
        var userData = GetUserData(cartridgeUid);
        if (userData == null)
            return;

        var server = GetServerForCartridge(cartridgeUid);
        if (server == null)
            return;

        var userId = userData.Value.Id;
        var userName = userData.Value.Name;
        var jobIconId = userData.Value.JobIconId;
        var jobTitle = userData.Value.JobTitle;

        // Checking for data matches
        if (server.Value.Component.Users.TryGetValue(userId, out var existing) && existing.Name == userName && existing.JobIconId == jobIconId && existing.JobTitle == jobTitle)
            return;

        server.Value.Component.Users[userId] = new MessagerUser(userId, userName, jobIconId, jobTitle);
        Dirty(server.Value.Uid, server.Value.Component);
    }

    /// <summary>
    /// Getting user data from Server UserList
    /// </summary>
    private (Dictionary<int, MessagerUserEntry> Users, MessagerStatus Status) GetUserList(EntityUid cartridgeUid)
    {
        var server = GetServerForCartridge(cartridgeUid);
        if (server == null)
            return (new Dictionary<int, MessagerUserEntry>(), MessagerStatus.ConnectionLost);

        var userData = GetUserData(cartridgeUid);
        var currentUserId = userData?.Id;

        var userList = server.Value.Component.Users
            .Where(kv => kv.Key != currentUserId)
            .Select(kv =>
            {
                var unreadCount = 0;
                if (currentUserId.HasValue)
                {
                    kv.Value.UnreadCounts.TryGetValue(currentUserId.Value, out unreadCount);
                }
                return new MessagerUserEntry(kv.Value.Id, kv.Value.Name, kv.Value.JobIconId, kv.Value.JobTitle, unreadCount);
            })
            .OrderByDescending(u => u.UnreadCount)
            .ToList();

        return (userList.ToDictionary(u => u.Id), MessagerStatus.Connected);
    }

    /// <summary>
    /// Takes messages from server and sends them to client
    /// </summary>
    private List<MessagerMessageEntry> GetMessages(EntityUid cartridgeUid)
    {
        var server = GetServerForCartridge(cartridgeUid);
        if (server == null)
            return new List<MessagerMessageEntry>();

        var userData = GetUserData(cartridgeUid);
        var currentUserId = userData?.Id ?? 0;

        var messages = new List<MessagerMessageEntry>();
        foreach (var msg in server.Value.Component.Messages)
        {
            var isIncoming = msg.ReceiverId == currentUserId || msg.SenderId == currentUserId;
            if (!isIncoming)
                continue;

            var senderName = server.Value.Component.Users.TryGetValue(msg.SenderId, out var sender)
                ? sender.Name
                : "Unknown";

            messages.Add(new MessagerMessageEntry(msg.Id, msg.Content, msg.Timestamp, msg.SenderId, msg.ReceiverId)
            {
                SenderName = senderName,
                IsIncoming = msg.SenderId != currentUserId
            });
        }

        return messages;
    }

    /// <summary>
    /// Processing messages from the client
    /// </summary>
    private void OnUiMessage(EntityUid uid, MessagerCartridgeComponent component, CartridgeMessageEvent args)
    {
        var userData = GetUserData(uid);
        if (userData == null)
            return;

        var server = GetServerForCartridge(uid);
        if (server == null)
            return;

        var loaderUid = GetLoaderUid(uid);
        if (loaderUid == null)
            return;

        if (args is MessagerSendMessageEvent sendMessage)
        {
            var messageId = server.Value.Component.Messages.Count > 0
                ? server.Value.Component.Messages.Max(m => m.Id) + 1
                : 1;

            var message = new MessagerMessage(
                messageId,
                userData.Value.Id,
                sendMessage.ReceiverId,
                sendMessage.Content,
                _gameTicker.RoundDuration()
            );

            server.Value.Component.Messages.Add(message);
            Dirty(server.Value.Uid, server.Value.Component);
            // Update sender UI
            UpdateUiState(uid, loaderUid.Value);

            var receiverCartridgeUid = GetCartridgeByUserId(sendMessage.ReceiverId);
            if (receiverCartridgeUid == null)
                return;

            var receiverLoaderUid = GetLoaderUid(receiverCartridgeUid.Value);
            if (receiverLoaderUid == null)
                return;

            SendNotificationToUser(receiverCartridgeUid.Value, userData.Value.Name, sendMessage.Content);

            if (server.Value.Component.Users.TryGetValue(userData.Value.Id, out var senderUser))
            {
                senderUser.UnreadCounts[sendMessage.ReceiverId] = senderUser.UnreadCounts.GetValueOrDefault(sendMessage.ReceiverId, 0) + 1;
                Dirty(server.Value.Uid, server.Value.Component);
            }
            // Update receiver UI
            UpdateUiState(receiverCartridgeUid.Value, receiverLoaderUid.Value);
        }

        if (args is MessagerRequestMessagesEvent requestMessages)
        {
            var currentUserData = GetUserData(uid);
            if (currentUserData != null)
            {
                var currentServer = GetServerForCartridge(uid);
                if (currentServer != null && currentServer.Value.Component.Users.TryGetValue(requestMessages.UserId, out var chatUser))
                {
                    chatUser.UnreadCounts[currentUserData.Value.Id] = 0;
                    Dirty(currentServer.Value.Uid, currentServer.Value.Component);
                }
            }
            UpdateUiState(uid, loaderUid.Value);
        }
    }

    private void SendNotificationToUser(EntityUid cartridgeUid, string senderName, string messagePreview)
    {
        var loaderUid = GetLoaderUid(cartridgeUid);
        if (loaderUid == null)
            return;

        var title = Loc.GetString("messager-notification-message", ("sender", senderName));
        _cartridgeLoaderSystem.SendNotification(loaderUid.Value, title, senderName + ": " + messagePreview);
    }


    private EntityUid? GetLoaderUid(EntityUid cartridgeUid)
    {
        if (!TryComp(cartridgeUid, out TransformComponent? transform))
            return null;

        return transform.ParentUid;
    }

    /// <summary>
    /// looking for the recipient's PDA
    /// </summary>
    private EntityUid? GetCartridgeByUserId(int userId)
    {
        var cartridgeQuery = EntityQueryEnumerator<MessagerCartridgeComponent>();
        while (cartridgeQuery.MoveNext(out var cartridgeUid, out _))
        {
            var userData = GetUserData(cartridgeUid);
            if (userData?.Id == userId)
                return cartridgeUid;
        }
        return null;
    }

    /// <summary>
    /// Getting user data from the IDcard
    /// </summary>
    public (int Id, string Name, string JobIconId, string JobTitle)? GetUserData(EntityUid cartridgeUid)
    {
        var pdaUid = GetLoaderUid(cartridgeUid);
        if (!TryComp<PdaComponent>(pdaUid, out var pda))
            return null;

        var idCardUid = pda.ContainedId;
        if (idCardUid == null)
            return null;

        if (!TryComp<IdCardComponent>(idCardUid, out var idCard))
            return null;

        var fullName = string.IsNullOrEmpty(idCard.FullName) ? Loc.GetString("generic-unknown") : idCard.FullName;
        var jobTitle = string.IsNullOrEmpty(idCard.LocalizedJobTitle) ? Loc.GetString("job-name-unknown") : idCard.LocalizedJobTitle;
        var id = (int)idCardUid.Value;
        return (id, fullName, idCard.JobIcon, jobTitle);
    }

    private void OnUiReady(EntityUid uid, MessagerCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader);
    }

    private void UpdateUiState(EntityUid cartridgeUid, EntityUid loaderUid)
    {
        // checking for an IDcard
        if (!TryComp<PdaComponent>(loaderUid, out var pda) || pda.ContainedId == null)
        {
            var lostState = new MessagerCartridgeUiState(MessagerStatus.ConnectionLost, new Dictionary<int, MessagerUserEntry>(), new List<MessagerMessageEntry>());
            _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, lostState);
            return;
        }

        SyncUsers();
        var (users, status) = GetUserList(cartridgeUid);
        var messages = GetMessages(cartridgeUid);
        var state = new MessagerCartridgeUiState(status, users, messages);
        _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, state);
    }
}
