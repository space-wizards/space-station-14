using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Content.Server.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader.Cartridges;
using Robust.Shared.Timing;
using Content.Shared.CartridgeLoader;
using Content.Server.CartridgeLoader;
using Content.Server.Radio.Components;
using Robust.Shared.Containers;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using System.Linq;


namespace Content.Server.Radio.EntitySystems;

public sealed class MessagesServerSystem : EntitySystem
{

    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MessagesCartridgeSystem _messagesCartridgeSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var serverQuery = EntityQueryEnumerator<MessagesServerComponent>();
        while (serverQuery.MoveNext(out var uid, out var server))
        {
            if (server.NextUpdate <= _gameTiming.CurTime)
            {
                server.NextUpdate += server.UpdateDelay;

                Update(uid, server);
            }
        }
    }

    public void Update(EntityUid uid, MessagesServerComponent component)
    {
        //<TODO> Sync messages between servers.
        var mapId = Transform(uid).MapID;

        if (!this.IsPowered(uid, EntityManager))
            return;

        var query = EntityManager.AllEntityQueryEnumerator<MessagesCartridgeComponent, CartridgeComponent>();

        Dictionary<int,List<(EntityUid, MessagesCartridgeComponent)>> cartDict = [];
        component.NameDict = [];

        while (query.MoveNext(out var cartUid, out var messagesCartComponent, out var cartComponent))
        {
            if (Transform(cartUid).MapID != mapId)
                continue;
            if (messagesCartComponent.EncryptionKey != component.EncryptionKey)
                continue;
            int? userUid = _messagesCartridgeSystem.GetUserUid(cartComponent);
            if (userUid == null)
                continue;
            if (!cartDict.ContainsKey(userUid.Value))
                cartDict[userUid.Value] = [];
            cartDict[userUid.Value].Add((cartUid, messagesCartComponent));
            component.NameDict[userUid.Value] = _messagesCartridgeSystem.GetUserName(cartComponent);
        }

        query = EntityManager.AllEntityQueryEnumerator<MessagesCartridgeComponent, CartridgeComponent>();

        //Loop iterates over all cartridges on the map when the server is updated
        while (query.MoveNext(out var cartUid, out var messagesCartComponent, out var cartComponent))
        {
            if (Transform(cartUid).MapID != mapId)
                continue;
            if (messagesCartComponent.EncryptionKey != component.EncryptionKey)
                continue;
            if (_messagesCartridgeSystem.GetUserUid(cartComponent) == null)
                continue;

            //if the cart has any unsent messages, the server attempts to send them
            while (messagesCartComponent.MessagesQueue.Count > 0)
            {
                var message = messagesCartComponent.MessagesQueue[0];
                TryToSend(message, mapId, cartDict);
                component.Messages.Add(message);
                messagesCartComponent.MessagesQueue.RemoveAt(0);
            }

            _messagesCartridgeSystem.ForceUpdate(cartUid, messagesCartComponent);
        }
    }

    ///<summary>
    ///Function that tries to send a message to any matching cartridges on its map
    ///</summary>
    public void TryToSend(MessagesMessageData message, MapId mapId, Dictionary<int,List<(EntityUid, MessagesCartridgeComponent)>> cartDict)
    {
        var cartList = cartDict[message.ReceiverId];

        foreach (var (cartUid, cartProgramComponent) in cartList)
        {
            var messagesCartComponent = CompOrNull<CartridgeComponent>(cartUid);
            if (messagesCartComponent == null || messagesCartComponent.LoaderUid == null)
                continue;
            _messagesCartridgeSystem.ServerToPdaMessage(cartUid, cartProgramComponent, message, messagesCartComponent.LoaderUid.Value);
        }
    }

    public string GetNameFromDict(EntityUid? uid, MessagesServerComponent? component, int key)
    {
        if ((uid == null) || (component == null))
            return "LOCALISE THIS TO SAY CONNECTION ERROR";
        if (component.NameDict.ContainsKey(key))
            return component.NameDict[key];
        return "LOCALISE THIS TO SAY UNKNOWN";
    }

    public Dictionary<int, string> GetNameDict(MessagesServerComponent component)
    {
        return component.NameDict;
    }

    public List<MessagesMessageData> GetMessages(MessagesServerComponent component,int id1, int id2)
    {
        return new List<MessagesMessageData>(component.Messages.Where(message => (message.SenderId == id1 && message.ReceiverId == id2) || (message.SenderId == id2 && message.ReceiverId == id1)));
    }

}
