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
using Content.Shared.Access.Systems;

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
    [Dependency] private readonly IdExaminableSystem _idExaminableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessagesCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<MessagesCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<MessagesCartridgeComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
        SubscribeLocalEvent<MessagesCartridgeComponent, CartridgeActivatedEvent>(OnCartActivation);
        SubscribeLocalEvent<MessagesCartridgeComponent, CartridgeDeactivatedEvent>(OnCartDeactivation);
        SubscribeLocalEvent<MessagesCartridgeComponent, CartridgeAddedEvent>(OnCartInsertion);
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(EntityUid uid, MessagesCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        var stationId = _stationSystem.GetOwningStation(uid);
        if (stationId.HasValue && _singletonServerSystem.TryGetActiveServerAddress<MessagesServerComponent>(stationId.Value, out var address) && TryComp(uid, out CartridgeComponent? cartComponent))
            SendName(uid, component, cartComponent, address);
        UpdateUiState(uid, component);
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
                component.ChatUid = messageEvent.TargetChatUid;
        }

        UpdateUiState(uid, component);
    }

    /// <summary>
    /// On cart insertion, register as background process.
    /// </summary>
    private void OnCartInsertion(EntityUid uid, MessagesCartridgeComponent component, CartridgeAddedEvent args)
    {
        _cartridgeLoaderSystem.RegisterBackgroundProgram(args.Loader, uid);
    }

    /// <summary>
    /// On cartridge activation, connect to messages network.
    /// </summary>
    private void OnCartActivation(EntityUid uid, MessagesCartridgeComponent component, CartridgeActivatedEvent args)
    {
        _deviceNetworkSystem.ConnectDevice(uid);
        var stationId = _stationSystem.GetOwningStation(uid);
        if (stationId.HasValue && _singletonServerSystem.TryGetActiveServerAddress<MessagesServerComponent>(stationId.Value, out var address) && TryComp(uid, out CartridgeComponent? cartComponent))
            SendName(uid, component, cartComponent, address);
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
        UpdateName(cartComponent);
        if (args.Data.TryGetValue<MessagesMessageData>("Message", out var message) && cartComponent.LoaderUid != null)
        {
            if (message.ReceiverId == GetUserUid(cartComponent))
            {
                TryGetName(message.SenderId, component, out var name);

                var subtitleString = Loc.GetString("messages-pda-notification-header");

                _cartridgeLoaderSystem.SendNotification(
                    cartComponent.LoaderUid.Value,
                    $"{name} {subtitleString}",
                    message.Content);
            }
        }

        UpdateUiState(uid, component);

    }

    /// <summary>
    /// Updates the user's name in the storage component.
    /// </summary>
    private void UpdateName(EntityUid uid, MessagesCartridgeComponent component, CartridgeComponent cartComponent, string? address)
    {
        TryGetUserName(cartComponent, out var name);
        var userUid = GetUserUid(cartComponent);
        var frequency = GetFrequency(uid);

        if (userUid !=)
    }

    /// <summary>
    /// Retrieves the name of the given user from the last contacted server
    /// </summary>
    private bool TryGetName(int key, MessagesCartridgeComponent component, out string name)
    {
        if (component.LastServer == null)
        {
            name = Loc.GetString("messages-pda-connection-error");
            return false;
        }
        return _messagesServerSystem.TryGetNameFromDict(component.LastServer, key, out name);
    }

    /// <summary>
    /// Returns the user's id in the messages system
    /// </summary>
    public int? GetUserUid(CartridgeComponent component)
    {
        var idCard = GetIdCard(component);
        if (idCard == null)
            return null;
        if (!TryComp(idCard, out IdCardComponent? idComponent))
            return null;
        return idComponent.MessagesId;
    }

    /// <summary>
    /// Returns the user's name and job title
    /// </summary>
    public bool TryGetUserName(CartridgeComponent component, out string name)
    {
        var idCard = GetIdCard(component);
        if (idCard == null)
        {
            name = Loc.GetString("messages-pda-unknown-name");
            return false;
        }
        var cardInfo = _idExaminableSystem.GetInfo(idCard.Value);
        if (cardInfo == null)
        {
            name = Loc.GetString("messages-pda-unknown-name");
            return false;
        }
        name = cardInfo;
        return true;
    }

    /// <summary>
    /// Finds the id card in the PDA if present
    /// </summary>
    private EntityUid? GetIdCard(CartridgeComponent component)
    {
        var loaderUid = component.LoaderUid;
        if (loaderUid == null ||
        !TryComp(loaderUid.Value, out PdaComponent? pdaComponent))
            return null;

        return pdaComponent.ContainedId;

    }

    ///<summary>
    ///Updates the ui state of a given cartridge
    ///</summary>
    public void ForceUpdate(EntityUid uid, MessagesCartridgeComponent component)
    {
        UpdateUiState(uid, component);
    }

    private void UpdateUiState(EntityUid uid, MessagesCartridgeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        if (!TryComp(uid, out CartridgeComponent? cartComponent))
            return;
        if (cartComponent.LoaderUid == null)
            return;
        var loaderUid = cartComponent.LoaderUid.Value;
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
                TryGetName(message.SenderId, component, out var name);
                var stationTime = message.Time.Subtract(_gameTicker.RoundStartTimeSpan);
                var content = $"{stationTime.ToString("\\[hh\\:mm\\:ss\\]")} {name}: {message.Content}";
                formattedMessageList.Add((content, null));
            }

            TryGetName(component.ChatUid.Value, component, out var chatterName);
            state = new MessagesUiState(MessagesUiStateMode.Chat, formattedMessageList, chatterName);
        }
        _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, state);
    }

}
