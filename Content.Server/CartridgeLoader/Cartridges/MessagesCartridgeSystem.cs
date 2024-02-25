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
    [Dependency] private readonly MessagesServerSystem _messagesServerSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly RingerSystem _ringer = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    //private ISawmill _sawmill = Logger.GetSawmill("pdaMessages");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessagesCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<MessagesCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        //_sawmill.Debug("System initialised");
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(EntityUid uid, MessagesCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        var mapId = Transform(uid).MapID;
        if ((component.ConnectedId != null) && (TryComp(component.ConnectedId, out IdCardComponent? idCardComponent)))
            component.UserName = idCardComponent.FullName+"("+idCardComponent.JobTitle+")";
            if (component.UserUid != null && component.UserName != null)
                component.NameDict[component.UserUid] = component.UserName;
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

        if ((messageEvent.Action == MessagesUiAction.Send) && (component.UserUid != null) && (component.ChatUid != null) && (messageEvent.Parameter != null))
        {
            MessagesMessageData messageData = new();
            messageData.SenderId = component.UserUid;
            messageData.ReceiverId = component.ChatUid;
            messageData.Content = messageEvent.Parameter;
            messageData.Time = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
            component.MessagesQueue.Add(messageData);
            Update(uid, component);
        }
        else
        {
            if (messageEvent.Action == MessagesUiAction.ChangeChat)
                component.ChatUid = messageEvent.Parameter;
        }

        UpdateUiState(uid, GetEntity(args.LoaderUid), component);
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var cartQuery = EntityQueryEnumerator<MessagesCartridgeComponent>();
        while (cartQuery.MoveNext(out var uid, out var messagesCartridge))
        {
            if (messagesCartridge.NextUpdate > _gameTiming.CurTime)
                continue;
            messagesCartridge.NextUpdate = _gameTiming.CurTime + messagesCartridge.UpdateDelay;

            Update(uid, messagesCartridge);
        }
    }

    public void Update(EntityUid uid, MessagesCartridgeComponent component)
    {
        var mapId = Transform(uid).MapID;

        if (component.UserName == null)
        {
            if ((component.ConnectedId != null) && (TryComp(component.ConnectedId, out IdCardComponent? idCardComponent)))
            {
                if (idCardComponent.FullName != "")
                {
                    component.UserName = $"{idCardComponent.FullName} {idCardComponent.JobTitle})";
                }
                else
                {
                    component.UserName = $"Unknown  ({idCardComponent.JobTitle})";
                }
            }
            if (component.UserUid != null && component.UserName != null)
                component.NameDict[component.UserUid] = component.UserName;
        }

        if ((component.MessagesQueue.Count > 0) || component.DeadConnection)
        {
            if (!(GetActiveServer(component, mapId) is var (serverUid, messageServer)))
            {
                component.DeadConnection=true;
                return;
            }
            else
            {
                if (component.DeadConnection)
                    PullFromServer(uid, component, messageServer);
                component.DeadConnection=false;
            }


            var tempMessageQueue = new List<MessagesMessageData>(component.MessagesQueue);
            foreach (var message in tempMessageQueue)
            {
                _messagesServerSystem.PdaToServerMessage(serverUid, messageServer,message);
                component.MessagesQueue.Remove(message);
                component.Messages.Add(message);
                UpdateUiState(uid, Transform(uid).ParentUid, component);
            }
        }
    }

    public (EntityUid, MessagesServerComponent)? GetActiveServer(MessagesCartridgeComponent component,MapId mapId)
    {
        var servers = EntityQueryEnumerator<MessagesServerComponent, ApcPowerReceiverComponent, TransformComponent>();
        while(servers.MoveNext(out var uid, out var messageServer, out var power, out var transform))
        {
            if (messageServer.EncryptionKey != component.EncryptionKey)
                continue;

            if (transform.MapID == mapId &&
                power.Powered)
            {
                return (uid, messageServer);
            }
        }
        return null;
    }

    private string GetName(MessagesCartridgeComponent component, string key)
    {
        if (!(component.NameDict.ContainsKey(key)))
        {
            return "Unknown";
        }
        else
        {
            if (component.NameDict[key][0] == '(')
            {
                return $"Unknown {component.NameDict[key]}";
            }
            else
            {
                return component.NameDict[key];
            }
        }
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, MessagesCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;
        MessagesUiState state;
        if (component.ChatUid == null)
        {
            List <(string,string)> userList = new();

            foreach (var nameEntry in component.NameDict.Keys)
            {
                if (nameEntry == component.UserUid)
                    continue;
                userList.Add((GetName(component,nameEntry),nameEntry));
            }

            userList.Sort(delegate((string,string) a, (string,string) b)
            {
                    return String.Compare(a.Item2,b.Item2);
            });

            state = new MessagesUiState(MessagesUiStateMode.UserList, userList, null);
        }
        else
        {
            List<MessagesMessageData> messageList = new();

            foreach (var message in component.Messages)
            {
                if ((message.SenderId == component.ChatUid && message.ReceiverId == component.UserUid) || (message.ReceiverId == component.ChatUid && message.SenderId == component.UserUid))
                {
                    messageList.Add(message);
                }
            }

            messageList.Sort(delegate(MessagesMessageData a, MessagesMessageData b)
            {
                return TimeSpan.Compare(a.Time,b.Time);
            });

            List<(string, string)> formattedMessageList = new();

            foreach (var message in messageList)
            {
                string name = GetName(component, message.SenderId);
                var stationTime =message.Time.Subtract(_gameTicker.RoundStartTimeSpan);
                string content = stationTime.ToString("\\[hh\\:mm\\:ss\\]")+" : "+message.Content;
                formattedMessageList.Add((name , content));
            }

            state = new MessagesUiState(MessagesUiStateMode.Chat, formattedMessageList, GetName(component, component.ChatUid));
        }
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }

    public void ServerToPdaMessage(EntityUid uid, MessagesCartridgeComponent component, MessagesMessageData message, EntityUid pdaUid, MessagesServerComponent server)
    {
        component.Messages.Add(message);

        string name = GetName(component, message.SenderId);

        var wrappedMessage = Loc.GetString("chat-radio-message-wrap",
            ("color", Color.White),
            ("fontType", "Default"),
            ("fontSize", 12),
            ("verb", "sends"),
            ("channel", "[PDA]"),
            ("name", name),
            ("message", message.Content));

        var chat = new ChatMessage(
            ChatChannel.Radio,
            message.Content,
            wrappedMessage,
            NetEntity.Invalid,
            null);

        var chatMsg = new MsgChatMessage { Message = chat };

        if (TryComp(Transform(pdaUid).ParentUid, out ActorComponent? actor))
            _netMan.ServerSendMessage(chatMsg, actor.PlayerSession.Channel);
        if (TryComp(pdaUid, out RingerComponent? ringer))
            _ringer.RingerPlayRingtone(pdaUid, ringer);


        if (TryComp(pdaUid, out CartridgeLoaderComponent? loaderUid))
            UpdateUiState(uid, pdaUid, component);
    }


    public void PullFromServer(EntityUid uid, MessagesCartridgeComponent component, MessagesServerComponent server)
    {
        //_sawmill.Debug("Syncing with server");

        if (component.ConnectedId == null)
            return;

        if (!(TryComp(component.ConnectedId, out IdCardComponent? idCardComponent)))
            return;

        component.UserName = $"{idCardComponent.FullName} ({idCardComponent.JobTitle})";

        if (server.NameDict != component.NameDict)
        {
            if ((component.UserUid is string userUid) && (component.UserName is string userName))
                server.NameDict[userUid]=userName;
            var keylist = server.NameDict.Keys;
            foreach (var key in keylist)
            {
                component.NameDict[key]=server.NameDict[key];
            }
        }

        //_sawmill.Debug("Pulling messages from server");

        foreach (var message in server.Messages)
        {
            if ((message.ReceiverId == component.UserUid || message.SenderId == component.UserUid) && !(component.Messages.Contains(message)))
                component.Messages.Add(message);
        }


    }
}
