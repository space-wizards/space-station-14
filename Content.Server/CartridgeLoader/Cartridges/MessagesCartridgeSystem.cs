using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.PDA;
using Robust.Shared.Map;
using Content.Server.Radio.Components;
using Content.Server.GameTicking;
using Robust.Shared.Timing;
using Content.Shared.Access.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork;
using Content.Server.Station.Systems;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class MessagesCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MessagesServerSystem _messagesServerSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServerSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessagesCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<MessagesCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<MessagesCartridgeComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<MessagesCartridgeComponent, CartridgeDeactivatedEvent>(OnCartDeactivation);
        SubscribeLocalEvent<MessagesCartridgeComponent, CartridgeActivatedEvent>(OnCartActivation);
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(EntityUid uid, MessagesCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        var stationId = _stationSystem.GetOwningStation(uid);
        if (stationId.HasValue)
            _singletonServerSystem.TryGetActiveServerAddress<MessagesServerComponent>(stationId.Value, out _);
        UpdateUiState(uid, args.Loader, component);
    }

    /// <summary>
    /// The ui messages received here get wrapped by a CartridgeMessageEvent and are relayed from the <see cref="CartridgeLoaderSystem"/>
    /// </summary>
    /// <remarks>
    /// The cartridge specific ui message event needs to inherit from the CartridgeMessageEvent
    /// </remarks>
    private void OnUiMessage(EntityUid uid, MessagesCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args is not MessagesUiMessageEvent messageEvent)
            return;

        if (messageEvent.Action == MessagesUiAction.Send && TryComp(uid, out CartridgeComponent? cartComponent) && GetUserUid(cartComponent) is int userId && component.ChatUid != null && messageEvent.StringInput != null)
        {
            var stationId = _stationSystem.GetOwningStation(uid);
            if (!stationId.HasValue)
                return;
            MessagesMessageData messageData = new()
            {
                SenderId = userId,
                ReceiverId = component.ChatUid.Value,
                Content = messageEvent.StringInput,
                Time = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan)
            };
            var packet = new NetworkPayload()
            {
                ["Message"] = messageData
            };
            _singletonServerSystem.TryGetActiveServerAddress<MessagesServerComponent>(stationId.Value, out var address);
            _deviceNetworkSystem.QueuePacket(uid, address, packet);
        }
        else
        {
            if (messageEvent.Action == MessagesUiAction.ChangeChat)
                component.ChatUid = messageEvent.UidInput;
        }

        UpdateUiState(uid, GetEntity(args.LoaderUid), component);
    }

    /// <summary>
    /// On cartridge activation, connect to messages network.
    /// </summary>
    private void OnCartActivation(EntityUid uid, MessagesCartridgeComponent component, CartridgeActivatedEvent args)
    {
        _deviceNetworkSystem.ConnectDevice(uid);
    }

    /// <summary>
    /// On cartridge deactivation, disconnect from messages network.
    /// </summary>
    private void OnCartDeactivation(EntityUid uid, MessagesCartridgeComponent component, CartridgeDeactivatedEvent args)
    {
        _deviceNetworkSystem.DisconnectDevice(uid, null);
    }

    /// <summary>
    /// React and respond to packets from the server
    /// </summary>
    private void OnPacketReceived(EntityUid uid, MessagesCartridgeComponent component, DeviceNetworkPacketEvent args)
    {
        if (!TryComp(uid, out CartridgeComponent? cartComponent))
            return;
        if (args.Data.TryGetValue<MessagesServerComponent>("ServerComponent", out var server))
            component.LastServer = server;
        if (args.Data.TryGetValue<bool>("NameQuery", out var _))
            SendName(uid, component, cartComponent, args.SenderAddress);
        if (args.Data.TryGetValue<MessagesMessageData>("Message", out var message) && cartComponent.LoaderUid != null)
        {
            if (message.ReceiverId == GetUserUid(cartComponent))
            {
                var name = GetName(message.SenderId, component);

                var subtitleString = Loc.GetString("messages-pda-notification-header");

                _cartridgeLoaderSystem.SendNotification(
                    cartComponent.LoaderUid.Value,
                    $"{name} {subtitleString} ",
                    message.Content);
            }

            if (HasComp<CartridgeLoaderComponent>(cartComponent.LoaderUid))
                UpdateUiState(uid, cartComponent.LoaderUid.Value, component);
        }

    }

    /// <summary>
    /// Sends the user's name to the server cache.
    /// </summary>
    private void SendName(EntityUid uid, MessagesCartridgeComponent component, CartridgeComponent cartComponent, string? address)
    {
        string name = GetUserName(cartComponent);

        var packet = new NetworkPayload()
        {
            ["UserId"] = GetUserUid(cartComponent),
            ["NewName"] = GetUserName(cartComponent),
        };
        _deviceNetworkSystem.QueuePacket(uid, address, packet);
    }

    /// <summary>
    /// Retrieves the name of the given user from the last contacted server
    /// </summary>
    private string GetName(int key, MessagesCartridgeComponent component)
    {
        if (component.LastServer == null)
            return Loc.GetString("messages-pda-connection-error");
        return _messagesServerSystem.GetNameFromDict(component.LastServer, key);
    }

    /// <summary>
    /// Returns the user's id in the messages system
    /// </summary>
    public int? GetUserUid(CartridgeComponent component)
    {
        var idComponent = GetIdCard(component);
        if (idComponent == null)
            return null;
        return idComponent.MessagesId;
    }

    /// <summary>
    /// Returns the user's name and job title
    /// </summary>
    public string GetUserName(CartridgeComponent component)
    {
        var idComponent = GetIdCard(component);
        string job;
        if (idComponent == null || idComponent.FullName == null)
            return Loc.GetString("messages-pda-unknown-name");
        if (idComponent.JobTitle != null)
        {
            job = idComponent.JobTitle;
        }
        else
        {
            job = Loc.GetString("messages-pda-unknown-job");
        }
        return $"{idComponent.FullName} ({job})";
    }

    /// <summary>
    /// Finds the id card in the PDA if present
    /// </summary>
    private IdCardComponent? GetIdCard(CartridgeComponent component)
    {
        var loaderUid = component.LoaderUid;
        if (loaderUid == null ||
        !TryComp(loaderUid.Value, out PdaComponent? pdaComponent))
            return null;

        return CompOrNull<IdCardComponent>(pdaComponent.ContainedId);

    }

    ///<summary>
    ///Updates the ui state of a given cartridge
    ///</summary>
    public void ForceUpdate(EntityUid uid, MessagesCartridgeComponent component)
    {
        if (HasComp<CartridgeLoaderComponent>(Transform(uid).ParentUid))
            UpdateUiState(uid, Transform(uid).ParentUid, component);
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, MessagesCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;
        if (!TryComp(uid, out CartridgeComponent? cartComponent))
            return;
        MessagesUiState state;
        MapId mapId = Transform(uid).MapID;
        int? currentUserId = GetUserUid(cartComponent);
        if (currentUserId == null || component.LastServer == null)
        {
            state = new MessagesUiState(MessagesUiStateMode.Error, [], null);
            _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, state);
            return;
        }
        if (component.ChatUid == null) //if no chat is loaded, list users
        {
            List<(string, int?)> userList = [];

            var nameDict = _messagesServerSystem.GetNameDict(component.LastServer);

            foreach (var nameEntry in nameDict.Keys)
            {
                if (nameEntry == currentUserId)
                    continue;
                userList.Add((nameDict[nameEntry], nameEntry));
            }

            userList.Sort
            (
                delegate ((string, int?) a, (string, int?) b)
                {
                    return String.Compare(a.Item1, b.Item1);
                }
            );

            state = new MessagesUiState(MessagesUiStateMode.UserList, userList, null);
        }
        else
        {
            List<MessagesMessageData> messageList = []; //Else, list messages from the current chat

            foreach (var message in _messagesServerSystem.GetMessages(component.LastServer, component.ChatUid.Value, currentUserId.Value))
            {
                if (message.SenderId == component.ChatUid && message.ReceiverId == currentUserId || message.ReceiverId == component.ChatUid && message.SenderId == currentUserId)
                {
                    messageList.Add(message);
                }
            }

            messageList.Sort
            (
                delegate (MessagesMessageData a, MessagesMessageData b)
                {
                    return TimeSpan.Compare(a.Time, b.Time);
                }
            );

            List<(string, int?)> formattedMessageList = [];

            foreach (var message in messageList)
            {
                var name = GetName(message.SenderId, component);
                var stationTime = message.Time.Subtract(_gameTicker.RoundStartTimeSpan);
                var content = $"{stationTime.ToString("\\[hh\\:mm\\:ss\\]")} {name}: {message.Content}";
                formattedMessageList.Add((content, null));
            }

            state = new MessagesUiState(MessagesUiStateMode.Chat, formattedMessageList, GetName(component.ChatUid.Value, component));
        }
        _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, state);
    }

}
