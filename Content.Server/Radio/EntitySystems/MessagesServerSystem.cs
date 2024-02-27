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
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
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
        List<(string,string)> toUpdate = new();

        bool needDictUpdate = false;

        while(query.MoveNext(out var cartUid, out var cartComponent))
        {
            if (Transform(cartUid).MapID != mapId)
                continue;
            if (cartComponent.EncryptionKey != component.EncryptionKey)
                continue;
            if (((cartComponent.UserUid == null) || (cartComponent.UserName == null)))
                _messagesCartridgeSystem.UpdateName(cartUid, cartComponent);
            if ((cartComponent.UserUid == null) || (cartComponent.UserName == null))
                continue;

            if (cartComponent.MessagesQueue.Count > 0)
            {
                var messagesToSend = new List<MessagesMessageData>(cartComponent.MessagesQueue);
                foreach (var message in messagesToSend)
                {
                    bool sent = TryToSend(message, mapId, component);
                    if (sent)
                    {
                        cartComponent.MessagesQueue.Remove(message);
                        cartComponent.Messages.Add(message);
                        component.Messages.Add(message);
                    }
                }
                _messagesCartridgeSystem.ForceUpdate(cartUid, cartComponent);
            }

            if ((component.NameDict.ContainsKey(cartComponent.UserUid)) && (component.NameDict[cartComponent.UserUid] == cartComponent.UserName))
                continue;

            needDictUpdate = true;
            component.NameDict[cartComponent.UserUid] = cartComponent.UserName;
            toUpdate.Add((cartComponent.UserUid,cartComponent.UserName));
            foreach (var entry in component.NameDict)
            {
                if (!cartComponent.NameDict.Keys.Contains(entry.Key) || cartComponent.NameDict[entry.Key] != entry.Value)
                {
                    cartComponent.NameDict[entry.Key] = entry.Value;
                }
            }
            _messagesCartridgeSystem.ForceUpdate(cartUid, cartComponent);
        }

        if (needDictUpdate)
        {
            query = _entManager.AllEntityQueryEnumerator<MessagesCartridgeComponent>();
            while (query.MoveNext(out var cartUid,out var cartComponent))
            {
                if (Transform(cartUid).MapID != mapId)
                    continue;
                if (((cartComponent.UserUid == null) || (cartComponent.UserName == null)) && !(_messagesCartridgeSystem.UpdateName(cartUid, cartComponent)))
                    continue;
                if (cartComponent.EncryptionKey != component.EncryptionKey)
                    continue;

                foreach (var (key,value) in toUpdate)
                {
                    cartComponent.NameDict[key]=value;
                }
                _messagesCartridgeSystem.ForceUpdate(cartUid, cartComponent);
            }
        }
    }

    public void Sync(EntityUid uid, MessagesServerComponent component)
    {
        var mapId = Transform(uid).MapID;
        var query = _entManager.AllEntityQueryEnumerator<MessagesCartridgeComponent>();
        while(query.MoveNext(out var cartUid, out var cartComponent))
        {
            if (Transform(cartUid).MapID != mapId)
                continue;
            if ((cartComponent.UserUid == null) || (cartComponent.UserName == null))
                continue;
            if (cartComponent.EncryptionKey != component.EncryptionKey)
                continue;

            foreach (var entry in component.NameDict)
            {
                if (cartComponent.NameDict.Keys.Contains(entry.Key) || cartComponent.NameDict[entry.Key] != entry.Value)
                {
                    cartComponent.NameDict[entry.Key] = entry.Value;
                }
            }
        }
    }

    public bool TryToSend(MessagesMessageData message, MapId mapId, MessagesServerComponent server)
    {
        bool sent = false;

        var query = EntityQueryEnumerator<CartridgeLoaderComponent, ContainerManagerComponent>();

        while (query.MoveNext(out var uid, out var comp, out var cont))
        {
            if (!_cartridgeLoaderSystem.TryGetProgram<MessagesCartridgeComponent>(uid, out var progUid, out var messagesCartridgeComponent, false, comp, cont))
                continue;
            if (progUid is EntityUid realProgUid)
            {
                if (messagesCartridgeComponent.UserUid == message.ReceiverId)
                    _messagesCartridgeSystem.ServerToPdaMessage(realProgUid, messagesCartridgeComponent, message, uid, server);
                sent = true;
            }
        }

        return sent;
    }

}
