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

        var query = EntityManager.AllEntityQueryEnumerator<MessagesCartridgeComponent>();

        Dictionary<int,List<(EntityUid, MessagesCartridgeComponent)>> cartDict = [];
        component.NameDict = [];

        while (query.MoveNext(out var cartUid, out var cartComponent))
        {
            if (Transform(cartUid).MapID != mapId)
                continue;
            if (cartComponent.EncryptionKey != component.EncryptionKey)
                continue;
            int? userUid = _messagesCartridgeSystem.GetUserUid(cartComponent);
            if (userUid == null)
                continue;
            if (!cartDict.ContainsKey(userUid.Value))
                cartDict[userUid.Value] = [];
            cartDict[userUid.Value].Add((cartUid, cartComponent));
            component.NameDict[userUid.Value] = _messagesCartridgeSystem.GetUserName(cartComponent);
        }

        query = EntityManager.AllEntityQueryEnumerator<MessagesCartridgeComponent>();

        //Loop iterates over all cartridges on the map when the server is updated
        while (query.MoveNext(out var cartUid, out var cartComponent))
        {
            if (Transform(cartUid).MapID != mapId)
                continue;
            if (cartComponent.EncryptionKey != component.EncryptionKey)
                continue;
            if (_messagesCartridgeSystem.GetUserUid(cartComponent) == null)
                continue;

            //if the cart has any unsent messages, the server attempts to send them
            if (cartComponent.MessagesQueue.Count > 0)
            {
                while(cartComponent.MessagesQueue.TryPop(out var message))
                {
                    TryToSend(message, mapId, cartDict);
                    component.Messages.Add(message);
                }
            }

            _messagesCartridgeSystem.ForceUpdate(cartUid, cartComponent);
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
            if (TryComp(cartUid, out CartridgeComponent cartComponent) && cartComponent.LoaderUid == null)
                continue;
            _messagesCartridgeSystem.ServerToPdaMessage(cartUid, cartProgramComponent, message, cartComponent.LoaderUid.Value);
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
        return component.Messages.Where(message => (message.SenderId == id1 && message.ReceiverId == id2) || (message.SenderId == id2 && message.ReceiverId == id1));
    }

}
