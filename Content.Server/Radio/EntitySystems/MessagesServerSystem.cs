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

namespace Content.Server.Radio.EntitySystems;

public sealed class MessagesServerSystem : EntitySystem
{

    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MessagesCartridgeSystem _messagesCartridgeSystem = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var serverQuery = EntityQueryEnumerator<MessagesServerComponent>();
        while (serverQuery.MoveNext(out var uid, out var server))
        {
            if (server.NextUpdate <= _gameTiming.CurTime)
            {
                server.NextUpdate = _gameTiming.CurTime + server.UpdateDelay;

                Update(uid, server);
            }
            if (server.NextSync <= _gameTiming.CurTime)
            {
                server.NextSync = _gameTiming.CurTime + server.SyncDelay;

                Sync(uid, server);
            }
        }
    }

    public void Update(EntityUid uid, MessagesServerComponent component)
    {
        var mapId = Transform(uid).MapID;

        if (!TryComp(uid, out ApcPowerReceiverComponent? powerReceiver) || !(powerReceiver.Powered))
            return;

        var query = _entManager.AllEntityQueryEnumerator<MessagesCartridgeComponent>();
        List<(int, string)> toUpdate = [];

        //Loop iterates over all cartridges on the map when the server is updated
        while (query.MoveNext(out var cartUid, out var cartComponent))
        {
            if (Transform(cartUid).MapID != mapId)
                continue;
            if (cartComponent.EncryptionKey != component.EncryptionKey)
                continue;
            if (cartComponent.UserUid == null || cartComponent.UserName == null)
                _messagesCartridgeSystem.UpdateName(cartUid, cartComponent);
            if (cartComponent.UserUid == null || cartComponent.UserName == null)
                continue;

            //if the cart has any unsent messages, the server attempts to send them
            if (cartComponent.MessagesQueue.Count > 0)
            {
                var messagesToSend = new List<MessagesMessageData>(cartComponent.MessagesQueue);
                foreach (var message in messagesToSend)
                {
                    bool sent = TryToSend(message, mapId);
                    if (sent)
                    {
                        cartComponent.MessagesQueue.Remove(message);
                        cartComponent.Messages.Add(message);
                        component.Messages.Add(message);
                    }
                }
                _messagesCartridgeSystem.ForceUpdate(cartUid, cartComponent);
            }

            //If the cart reports a changed name, it adds it to the toUpdate list and updates the cart's name dictionary
            if (component.NameDict.TryGetValue(cartComponent.UserUid.Value, out var cartUserName) && cartUserName == cartComponent.UserName)
                continue;

            component.NameDict[cartComponent.UserUid.Value] = cartComponent.UserName;
            toUpdate.Add((cartComponent.UserUid.Value, cartComponent.UserName));
            foreach (var entry in component.NameDict)
            {
                if (!cartComponent.NameDict.TryGetValue(entry.Key, out var targetUserName) || targetUserName != entry.Value)
                {
                    cartComponent.NameDict[entry.Key] = entry.Value;
                }
            }
            _messagesCartridgeSystem.ForceUpdate(cartUid, cartComponent);
        }

        //If any names were changed or added, the server updates all the carts on its map.
        if (toUpdate.Count > 0)
        {
            query = _entManager.AllEntityQueryEnumerator<MessagesCartridgeComponent>();
            while (query.MoveNext(out var cartUid, out var cartComponent))
            {
                if (Transform(cartUid).MapID != mapId)
                    continue;
                if ((cartComponent.UserUid == null || cartComponent.UserName == null) && !(_messagesCartridgeSystem.UpdateName(cartUid, cartComponent)))
                    continue;
                if (cartComponent.EncryptionKey != component.EncryptionKey)
                    continue;

                foreach (var (key, value) in toUpdate)
                {
                    cartComponent.NameDict[key] = value;
                }
                _messagesCartridgeSystem.ForceUpdate(cartUid, cartComponent);
            }
        }
    }

    //Sync function that updates the name dictionaries of all carts to match the server.
    //Called periodically with update.
    public void Sync(EntityUid uid, MessagesServerComponent component)
    {
        var mapId = Transform(uid).MapID;
        var query = _entManager.AllEntityQueryEnumerator<MessagesCartridgeComponent>();
        while (query.MoveNext(out var cartUid, out var cartComponent))
        {
            if (Transform(cartUid).MapID != mapId)
                continue;
            if (cartComponent.UserUid == null || cartComponent.UserName == null)
                continue;
            if (cartComponent.EncryptionKey != component.EncryptionKey)
                continue;

            foreach (var entry in component.NameDict)
            {
                if (cartComponent.NameDict.ContainsKey(entry.Key) || cartComponent.NameDict[entry.Key] != entry.Value)
                {
                    cartComponent.NameDict[entry.Key] = entry.Value;
                }
            }
        }
    }

    //function that tries to send a message to any matching cartridges on its map
    public bool TryToSend(MessagesMessageData message, MapId mapId)
    {
        bool sent = false;

        var query = EntityQueryEnumerator<MessagesCartridgeComponent, CartridgeComponent>();

        while (query.MoveNext(out var uid, out var messagesCartridgeComponent, out var cartridge))
        {
            if (Transform(uid).MapID != mapId)
                continue;
            if (cartridge.LoaderUid != null) //<TODO> this should probably be more generalisable
            {
                if (messagesCartridgeComponent.UserUid == message.ReceiverId)
                    _messagesCartridgeSystem.ServerToPdaMessage(uid, messagesCartridgeComponent, message, cartridge.LoaderUid.Value);
                sent = true;
            }
        }

        return sent;
    }

}
