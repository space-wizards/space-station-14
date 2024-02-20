using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class MessagesCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly MessageServerSystem? _messageServerSystem = default!;

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
        UpdateId(uid, component);
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

        if (messageEvent.Action == MessagesUiAction.Send)
        {
            MessagesMessageData messageData = new();
            messageData.SenderId = component.UserUid;
            messageData.ReceiverId = component.ChatUid;
            messageData.Content = messageEvent.Parameter;
            messageData.Time = 0.0; ///<TODO> add actual timekeeping here
            component.MessagesQueue.Add(messageData);
            Update(uid, component);
        }
        else
        {
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
        var messageServer = GetActiveServer(mapId);

        if ((component.MessagesQueue.Count > 0) && (messageServer != null))
        {
            foreach (var message in component.MessagesQueue)
            {
                _messageServerSystem.PdaToServerMessage(messageServer,message);
                component.MessagesQueue.Remove(message);
                component.Messages.Add(message);
                ///<TODO> Update UI here
            }
        }
    }

    private MessageServerComponent? GetActiveServer(MapId mapId)
    {
        var servers = EntityQuery<MessageServerComponent, ApcPowerReceiverComponent, TransformComponent>();
        foreach (var (messageServer, power, transform) in servers)
        {
            if (transform.MapID == mapId &&
                power.Powered)
            {
                return messageServer;
            }
        }
        return null;
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, MessagesCartridgeComponent? component)
    {
        if (!Resolve(uid, ref component))
            return;

        var state = new MessagesUiState(component.Messages,component.Chat);
        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }

    public void ServerToPdaMessage(EntityUid uid, MessagesCartridgeComponent component, MessagesMessageData message)
    {
        component.Messages.Add(message);

        var wrappedMessage = Loc.GetString("chat-radio-message-wrap",
            ("color", Color.White),
            ("fontType", "Default"),
            ("fontSize", 12),
            ("verb", "sends")),
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

        if (TryComp(Transform(uid).ParentUid, out ActorComponent? actor))
            _netMan.ServerSendMessage(chatMsg, actor.PlayerSession.Channel);
        if (TryComp(uid, RingerComponent? ringer))
            _ringer.RingerPlayRingtone(uid, ringer);

        ///<TODO> Update UI here
    }

    public void SyncWithServer(EntityUid uid, MessagesCartridgeComponent component, MessagesServerComponent server)
    {
        for (key, value in )
    }
}
