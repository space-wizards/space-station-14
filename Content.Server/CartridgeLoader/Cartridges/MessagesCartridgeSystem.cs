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

        if (messageEvent.Action == MessagesUiAction.Send && component.UserUid != null && component.ChatUid != null && messageEvent.StringInput != null)
        {
            MessagesMessageData messageData = new()
            {
                SenderId = component.UserUid.Value,
                ReceiverId = component.ChatUid.Value,
                Content = messageEvent.StringInput,
                Time = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan)
            };
            component.MessagesQueue.Add(messageData);
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
    public (EntityUid, MessagesServerComponent)? GetActiveServer(MessagesCartridgeComponent component, MapId mapId)
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
        return null;
    }

    //helper function to get name of a given user
    private static string GetName(int key)
    {
        return "LAZY RETURN PLEASE ADD IDENTITY SUPPORT, FLESH YOU GIT";
    }

    //helper function to get messages id of a given cart
    private static int GetUserUid(MessagesCartridgeComponent component)
    {
        return -1;
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
        if (component.ChatUid == null) //if no chat is loaded, list known users
        {
            List<(string, int?)> userList = [];

            foreach (var nameEntry in component.NameDict.Keys)
            {
                if (nameEntry == component.UserUid)
                    continue;
                userList.Add((GetName(component, nameEntry), nameEntry));
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

            foreach (var message in component.Messages)
            {
                if (message.SenderId == component.ChatUid && message.ReceiverId == component.UserUid || message.ReceiverId == component.ChatUid && message.SenderId == component.UserUid)
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
                string name = GetName(component, message.SenderId);
                var stationTime = message.Time.Subtract(_gameTicker.RoundStartTimeSpan);
                string content = $"{stationTime.ToString("\\[hh\\:mm\\:ss\\]")} {name}: {message.Content}";
                formattedMessageList.Add((content, null));
            }

            state = new MessagesUiState(MessagesUiStateMode.Chat, formattedMessageList, GetName(component, component.ChatUid.Value));
        }
        _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, state);
    }

    //function that receives the message and notifies the user
    public void ServerToPdaMessage(EntityUid uid, MessagesCartridgeComponent component, MessagesMessageData message, EntityUid pdaUid)
    {
        component.Messages.Add(message);

        string name = GetName(component, message.SenderId);

        var subtitleString = Loc.GetString("messages-pda-notification-header");

        _cartridgeLoaderSystem.SendNotification(
            pdaUid,
            $"{name} {subtitleString} ",
            message.Content);

        if (TryComp(pdaUid, out CartridgeLoaderComponent? _))
            UpdateUiState(uid, pdaUid, component);
    }

    //Function that downloads the dictionary and messages from the server
    public void PullFromServer(EntityUid uid, MessagesCartridgeComponent component, MessagesServerComponent server)
    {

        if (component.ConnectedId == null)
            return;

        if (!(HasComp<IdCardComponent>(component.ConnectedId)))
            return;

        UpdateName(uid, component);

        if (server.NameDict != component.NameDict)
        {
            var keylist = server.NameDict.Keys;
            foreach (var key in keylist)
            {
                if (key == component.UserUid)
                    continue;
                component.NameDict[key] = server.NameDict[key];
            }
        }

        foreach (var message in server.Messages)
        {
            if ((message.ReceiverId == component.UserUid || message.SenderId == component.UserUid) && !(component.Messages.Contains(message)))
                component.Messages.Add(message);
        }


    }
}
