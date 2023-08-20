// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Messenger;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Database;
using Content.Shared.Messenger;
using Content.Shared.PDA.Ringer;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class MessengerClientCartridgeSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem? _deviceNetworkSystem = default!;
    [Dependency] private readonly MessengerServerSystem _messengerServerSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ChatSystem? _chat = default;

    public enum NetworkCommand
    {
        CheckServer,
        StateUpdate,
        MessageSend,
    }

    public enum NetworkKey
    {
        Command,
        DeviceUid,
        ChatId,
        MessageText,
        CurrentChatIds,
        ContactsIds,
        MessagesIds,
    }


    // queue for ui states, if sent too often, then some states are lost,
    // send one state per update
    private readonly Queue<QueueBoundUserInterfaceState> _statesQueue = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessengerClientCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<MessengerClientCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<MessengerClientCartridgeComponent, CartridgeAddedEvent>(OnInstall);
        SubscribeLocalEvent<MessengerClientCartridgeComponent, DeviceNetworkPacketEvent>(OnNetworkPacket);
    }

    public override void Update(float frameTime)
    {
        // send one state per update
        if (_statesQueue.TryDequeue(out var state))
        {
            if (state.State != null)
                _cartridgeLoaderSystem?.UpdateCartridgeUiState(state.LoaderUid, state.State);
        }

        // is component request full state, try to get it from server
        foreach (var clientCartridgeComponent in _entityManager.EntityQuery<MessengerClientCartridgeComponent>())
        {
            if (!clientCartridgeComponent.SendState)
                continue;

            if (clientCartridgeComponent.ActiveServer == null)
                continue;

            if (!_entityManager.TryGetComponent<MessengerServerComponent>(
                    clientCartridgeComponent.ActiveServer.Value, out var server))
                continue;

            if (!_messengerServerSystem.RestoreContactUIStateIdCard(clientCartridgeComponent.Loader, ref server,
                    out var messengerUiState))
                continue;

            UpdateMessengerUiState(clientCartridgeComponent.Loader, messengerUiState);
            clientCartridgeComponent.SendState = false;
        }
    }

    private void OnNetworkPacket(EntityUid uid, MessengerClientCartridgeComponent? component,
        DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(MessengerServerSystem.NetworkKey.Command.ToString(),
                out MessengerServerSystem.NetworkCommand? command))
            return;

        if (!Resolve(uid, ref component))
            return;

        switch (command)
        {
            // when receive msg about server info add this server to servers list, and if received add server name
            case MessengerServerSystem.NetworkCommand.Info:
            {
                var serverInfo = new ServerInfo();

                if (args.Data.TryGetValue(MessengerServerSystem.NetworkKey.ServerName.ToString(),
                        out string? serverName))
                    serverInfo.Name = serverName;

                serverInfo.Address = args.SenderAddress;

                component.ActiveServer ??= args.Sender;

                if (!component.Servers.TryAdd(args.Sender, serverInfo))
                {
                    component.Servers.Remove(args.Sender);
                    component.Servers.Add(args.Sender, serverInfo);
                }

                component.SendState = true;
                break;
            }
            // receive client contact info
            case MessengerServerSystem.NetworkCommand.ClientContact:
            {
                if (!args.Data.TryGetValue(MessengerServerSystem.NetworkKey.Contact.ToString(),
                        out MessengerContact? contact))
                    break;

                UpdateMessengerUiState(component.Loader, new MessengerClientContactUiState(contact));
                break;
            }
            // receive contact info
            case MessengerServerSystem.NetworkCommand.Contact:
            {
                var contactsUpdate = new List<MessengerContact>();

                if (args.Data.TryGetValue(MessengerServerSystem.NetworkKey.Contact.ToString(),
                        out MessengerContact? contact))
                {
                    contactsUpdate.Add(contact);
                }

                if (args.Data.TryGetValue(MessengerServerSystem.NetworkKey.ContactList.ToString(),
                        out List<MessengerContact>? contacts))
                {
                    contactsUpdate.AddRange(contacts);
                }

                if (contactsUpdate.Count > 0)
                    UpdateMessengerUiState(component.Loader, new MessengerContactUiState(contactsUpdate));
                break;
            }
            // receive chat info
            case MessengerServerSystem.NetworkCommand.Chat:
            {
                var updateChats = new List<MessengerChat>();

                if (args.Data.TryGetValue(MessengerServerSystem.NetworkKey.Chat.ToString(), out MessengerChat? chat))
                {
                    updateChats.Add(chat);
                }

                if (args.Data.TryGetValue(MessengerServerSystem.NetworkKey.ChatList.ToString(),
                        out List<MessengerChat>? chats))
                {
                    updateChats.AddRange(chats);
                }

                if (updateChats.Count > 0)
                    UpdateMessengerUiState(component.Loader, new MessengerChatUpdateUiState(updateChats));
                break;
            }
            // receive message info
            case MessengerServerSystem.NetworkCommand.Messages:
            {
                var updateMessages = new List<MessengerMessage>();

                if (args.Data.TryGetValue(MessengerServerSystem.NetworkKey.Message.ToString(),
                        out MessengerMessage? message))
                {
                    updateMessages.Add(message);
                }

                if (args.Data.TryGetValue(MessengerServerSystem.NetworkKey.MessageList.ToString(),
                        out List<MessengerMessage>? messages))
                {
                    updateMessages.AddRange(messages);
                }

                if (updateMessages.Count > 0)
                    UpdateMessengerUiState(component.Loader, new MessengerMessagesUiState(updateMessages));

                break;
            }
            // receive new chat message
            case MessengerServerSystem.NetworkCommand.NewMessage:
            {
                if (!args.Data.TryGetValue(MessengerServerSystem.NetworkKey.Message.ToString(),
                        out MessengerMessage? message))
                    break;
                if (!args.Data.TryGetValue(MessengerServerSystem.NetworkKey.ChatId.ToString(), out uint? chatId))
                    break;

                RaiseLocalEvent(component.Loader, new RingerPlayRingtoneMessage());

                UpdateMessengerUiState(component.Loader, new MessengerNewChatMessageUiState(chatId.Value, message));
                break;
            }
        }
    }

    private void OnInstall(EntityUid uid, MessengerClientCartridgeComponent component, CartridgeAddedEvent args)
    {
        component.Loader = args.Loader;

        if (component.IsInstalled)
            return;

        component.IsInstalled = true;

        BroadcastCommand(uid, args.Loader, NetworkCommand.CheckServer);
    }

    private void BroadcastCommand(EntityUid senderUid, EntityUid loaderUid, NetworkCommand command)
    {
        var deviceMapId = Transform(loaderUid).MapID;
        var activeServersFrequency = _messengerServerSystem.ActiveServersFrequency(deviceMapId);

        foreach (var frequency in activeServersFrequency)
        {
            _deviceNetworkSystem?.QueuePacket(senderUid, null, new NetworkPayload
            {
                [NetworkKey.Command.ToString()] = command,
                [NetworkKey.DeviceUid.ToString()] = loaderUid
            }, frequency: frequency);
        }
    }

    private void OnUiReady(EntityUid uid, MessengerClientCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        if (component.ActiveServer != null)
            return;

        BroadcastCommand(uid, args.Loader, NetworkCommand.CheckServer);
        UpdateMessengerUiState(args.Loader,
            new MessengerErrorUiState(Loc.GetString("messenger-error-server-not-found")));
    }

    private void OnUiMessage(EntityUid uid, MessengerClientCartridgeComponent? component, CartridgeMessageEvent args)
    {
        if (!Resolve(uid, ref component))
            return;

        var serverAddress = ServerAddress(ref component);

        switch (args)
        {
            // to sync state on UI and on server, client send current state,
            // and if server decide it will send missing state
            case MessengerUpdateStateUiEvent e:
            {
                // client could request full state sync
                if (e.IsFullState)
                {
                    component.SendState = true;
                    break;
                }

                var payload = new NetworkPayload
                {
                    [NetworkKey.Command.ToString()] = NetworkCommand.StateUpdate,
                    [NetworkKey.DeviceUid.ToString()] = args.LoaderUid,
                };

                if (e.CurrentContacts is { Count: > 0 })
                    payload[NetworkKey.ContactsIds.ToString()] = e.CurrentContacts;

                if (e.CurrentMessages is { Count: > 0 })
                    payload[NetworkKey.MessagesIds.ToString()] = e.CurrentMessages;

                if (e.CurrentChats is { Count: > 0 })
                    payload[NetworkKey.CurrentChatIds.ToString()] = e.CurrentChats;

                _deviceNetworkSystem?.QueuePacket(uid, serverAddress, payload);

                break;
            }
            case MessengerSendMessageUiEvent e:
            {
                if (e.MessageText == "")
                    break;

                var message = _chat?.ReplaceWords(e.MessageText);

                _deviceNetworkSystem?.QueuePacket(uid, serverAddress, new NetworkPayload
                {
                    [NetworkKey.Command.ToString()] = NetworkCommand.MessageSend,
                    [NetworkKey.DeviceUid.ToString()] = args.LoaderUid,
                    [NetworkKey.ChatId.ToString()] = e.ChatId,
                    [NetworkKey.MessageText.ToString()] = message ?? "",
                });

                _adminLogger.Add(LogType.MessengerClientCartridge, LogImpact.Low,
                    $"Send: sender entity: {uid}, device entity: {args.LoaderUid}, chatID: {e.ChatId}, msg: {e.MessageText}, filtered: {message}");

                break;
            }
        }
    }

    private void UpdateMessengerUiState(EntityUid loaderUid, BoundUserInterfaceState messengerUiState)
    {
        _statesQueue.Enqueue(new QueueBoundUserInterfaceState(loaderUid, messengerUiState));
    }

    private static string? ServerAddress(ref MessengerClientCartridgeComponent component)
    {
        return component.ActiveServer == null ? null : component.Servers[component.ActiveServer.Value].Address;
    }
}

public sealed class QueueBoundUserInterfaceState
{
    public EntityUid LoaderUid;
    public readonly BoundUserInterfaceState? State;

    public QueueBoundUserInterfaceState(EntityUid loaderUid, BoundUserInterfaceState? state)
    {
        LoaderUid = loaderUid;
        State = state;
    }
}
