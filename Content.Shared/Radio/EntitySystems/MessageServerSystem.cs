using Robust.Server.GameObjects;

namespace Content.Shared.Radio.Components;


public sealed class MessagesServerSystem : EntitySystem
{

    private override void Update(float frameTime)
    {
        base.Update(frameTime);
        var serverQuery = EntityQueryEnumerator<MessagesServerComponent>();
        while (serverQuery.MoveNext(out var uid, out var server))
        {
            if (server.NextUpdate > _gameTiming.CurTime)
                continue;
            server.NextUpdate = _gameTiming.CurTime + server.UpdateDelay;

            Update(uid, server);
        }
    }

    public void Update(EntityUid uid, MessagesServerComponent component)
    {
        var mapId = Transform(uid).MapID;

        if ((component.MessagesQueue.Count > 0))
        {
            foreach (var message in component.MessagesQueue)
            {
                bool sent=TryToSend(message);
                if (sent) component.MessagesQueue.Remove(message, mapId);
            }
        }
    }

    public bool TryToSend(MessagesMessageData message, int mapId)
    {
        bool sent = false;

        var query = EntityQueryEnumerator<MessagesCartridgeComponent, TransformComponent>();

        foreach (var (uid, cartridge,transform) in query)
        {
            if (cartridge.UserUid == message.ReceiverUid) && (transform.MapID == mapId)
            {
                _messagesCartridgeSystem.ServerToPdaMessage(uid, cartridge, message);
            }
        }

        return sent;
    }

    public void PdaToServerMessage(MessagesServerComponent component, MessagesMessageData message)
    {
        component.Messages.Add(message);
        component.MessagesQueue.Add(message);
        /// <TODO> Add Update()
    }

}
