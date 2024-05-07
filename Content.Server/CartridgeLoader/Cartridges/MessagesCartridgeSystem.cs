using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.PDA;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Content.Server.Radio.Components;
using Content.Server.GameTicking;
using Robust.Shared.Timing;
using Content.Shared.Access.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Power.Components;
using Content.Shared.Chat;
using Robust.Shared.Player;
using Content.Server.PDA.Ringer;
using Robust.Shared.Network;


namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class MessagesCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MessagesServerSystem _messagesServerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessagesCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<MessagesCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(EntityUid uid, MessagesCartridgeComponent component, CartridgeUiReadyEvent args)
    {
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

        if (messageEvent.Action == MessagesUiAction.Send && GetUserUid(component) != null && component.ChatUid != null && messageEvent.StringInput != null)
        {
            MessagesMessageData messageData = new()
            {
                SenderId = GetUserUid(component).Value,
                ReceiverId = component.ChatUid.Value,
                Content = messageEvent.StringInput,
                Time = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan)
            };
            component.MessagesQueue.Push(messageData);
        }
        else
        {
            if (messageEvent.Action == MessagesUiAction.ChangeChat)
                component.ChatUid = messageEvent.UidInput;
        }

        UpdateUiState(uid, GetEntity(args.LoaderUid), component);
    }

    //Function that returns the uid and component of an active server with matching faction on a given map if it exists
    //<Todo> might be better to move this to the server system
    public (EntityUid?, MessagesServerComponent?) GetActiveServer(MessagesCartridgeComponent component, MapId mapId)
    {
        var servers = EntityManager.AllEntityQueryEnumerator<MessagesServerComponent, ApcPowerReceiverComponent, TransformComponent>();
        while (servers.MoveNext(out var uid, out var messageServer, out var power, out var transform))
        {
            if (messageServer.EncryptionKey != component.EncryptionKey)
                continue;

            if (transform.MapID == mapId &&
                power.Powered)
            {
                return (uid, messageServer);
            }
        }
        return (null,null);
    }

    //helper function to get name of a given user
    private string GetName(int key, MessagesCartridgeComponent component, MapId mapId)
    {
        var serverSearch = GetActiveServer(component, mapId);
        return _messagesServerSystem.GetNameFromDict(serverSearch.Item1, serverSearch.Item2, key);
    }

    //helper function to get messages id of a given cart
    public int? GetUserUid(MessagesCartridgeComponent component)
    {
        return -1; //<TODO> Actually fix this.
    }

    public string GetUserName(MessagesCartridgeComponent component)
    {
        return "STUB";//<TODO> IMPLEMENT IDENTITY
    }

    //Updates the ui state of a given cartridge
    public void ForceUpdate(EntityUid uid, MessagesCartridgeComponent component)
    {
        if (TryComp(Transform(uid).ParentUid, out CartridgeLoaderComponent? _))
            UpdateUiState(uid, Transform(uid).ParentUid, component);
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, MessagesCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;
        MessagesUiState state;
        MapId mapId = Transform(uid).MapID;
        int? currentUserId = GetUserUid(component);
        var serverSearch = GetActiveServer(component, mapId);
        if ((currentUserId == null) || (serverSearch.Item2 == null))
        {
            state = new MessagesUiState(MessagesUiStateMode.Error,null,null);
            _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, state);
            return;
        }
        if (component.ChatUid == null) //if no chat is loaded, list users
        {
            List<(string, int?)> userList = [];

            var nameDict = _messagesServerSystem.GetNameDict(serverSearch.Item2);

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

            foreach (var message in _messagesServerSystem.GetMessages(serverSearch.Item2, component.ChatUid, currentUserId))
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
                string name = GetName(message.SenderId, component, mapId);
                var stationTime = message.Time.Subtract(_gameTicker.RoundStartTimeSpan);
                string content = $"{stationTime.ToString("\\[hh\\:mm\\:ss\\]")} {name}: {message.Content}";
                formattedMessageList.Add((content, null));
            }

            state = new MessagesUiState(MessagesUiStateMode.Chat, formattedMessageList, GetName(component.ChatUid.Value, component, mapId));
        }
        _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, state);
    }

    //function that receives the message and notifies the user
    public void ServerToPdaMessage(EntityUid uid, MessagesCartridgeComponent component, MessagesMessageData message, EntityUid pdaUid)
    {
        string name = GetName(message.SenderId, component, Transform(uid).MapID);

        var subtitleString = Loc.GetString("messages-pda-notification-header");

        _cartridgeLoaderSystem.SendNotification(
            pdaUid,
            $"{name} {subtitleString} ",
            message.Content);

        if (HasComp<CartridgeLoaderComponent>(pdaUid))
            UpdateUiState(uid, pdaUid, component);
    }

}
