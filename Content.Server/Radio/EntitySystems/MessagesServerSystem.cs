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
        var mapId = Transform(uid).MapID;

        if (this.IsPowered(uid, EntityManager))
            return;

        var query = EntityManager.AllEntityQueryEnumerator<MessagesCartridgeComponent>();

        Dictionary<int,List<MessagesCartridgeComponent>> cartDict = [];

        while (query.MoveNext(out var cartUid, out var cartComponent))
        {
            if (Transform(cartUid).MapID != mapId)
                continue;
            if (cartComponent.EncryptionKey != component.EncryptionKey)
                continue;
            int userUid = cartComponent.GetUserUid();
            if (userUid == null)
                continue;
            if (!cartDict.HasKey(userUid))
                cartDict[userUid] = [];
            cartDict[userUid].Append(cartComponent);
        }

        query = EntityManager.AllEntityQueryEnumerator<MessagesCartridgeComponent>();

        //Loop iterates over all cartridges on the map when the server is updated
        while (query.MoveNext(out var cartUid, out var cartComponent))
        {
            if (Transform(cartUid).MapID != mapId)
                continue;
            if (cartComponent.EncryptionKey != component.EncryptionKey)
                continue;
            if (cartComponent.GetUserUid() == null)
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
    public void TryToSend(MessagesMessageData message, MapId mapId, Dictionary<int,List<MessagesCartridgeComponent>> cartDict)
    {
        var cartList = cartDict[message.ReceiverId];

        foreach (var cart in cartList)
        {
            EntityUid uid = EntityUid<MessagesCartridgeComponent>(cart);
            if (cart.LoaderUid == null)
                continue;
            _messagesCartridgeSystem.ServerToPdaMessage(uid, cart, message, cartridge.LoaderUid.Value);
        }
    }

}
