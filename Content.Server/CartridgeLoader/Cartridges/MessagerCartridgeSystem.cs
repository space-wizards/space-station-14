using System.Linq;
using Content.Server.Radio.EntitySystems;
using Content.Shared.PDA;
using Content.Shared.Access.Components;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Radio.Components;
using Content.Server.Power.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class MessagerCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridgeLoaderSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessagerCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<MessagerCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        SyncUsers();
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

        // Checking for data matches
        if (server.Value.Component.Users.TryGetValue(userId, out var existing) && existing.Name == userName)
            return;

        server.Value.Component.Users[userId] = new MessagerUser(userId, userName);
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

        return (server.Value.Component.Users
            .ToDictionary(k => k.Key, v => new MessagerUserEntry(v.Value.Id, v.Value.Name)), MessagerStatus.Connected);
    }

    private void OnUiMessage(EntityUid uid, MessagerCartridgeComponent component, CartridgeMessageEvent args)
    {
        // Обработка сообщений от UI
    }

    /// <summary>
    /// Getting user data from the IDcard
    /// </summary>
    public (int Id, string Name)? GetUserData(EntityUid cartridgeUid)
    {
        if (!TryComp(cartridgeUid, out TransformComponent? transform))
            return null;

        var pdaUid = transform.ParentUid;
        if (!TryComp<PdaComponent>(pdaUid, out var pda))
            return null;

        var idCardUid = pda.ContainedId;
        if (idCardUid == null)
            return null;

        if (!TryComp<IdCardComponent>(idCardUid, out var idCard))
            return null;

        var fullName = idCard.FullName;
        if (string.IsNullOrEmpty(fullName))
            fullName = "Unknown";

        var id = (int)idCardUid.Value;
        return (id, fullName);
    }

    private void OnUiReady(EntityUid uid, MessagerCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        var (users, status) = GetUserList(uid);
        UpdateUiState(args.Loader, status, users);
    }

    private void UpdateUiState(EntityUid loaderUid, MessagerStatus status, Dictionary<int, MessagerUserEntry> users)
    {
        var state = new MessagerCartridgeUiState(status, users);
        _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, state);
    }
}
