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
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork;


namespace Content.Server.Radio.EntitySystems;

public sealed class MessagesServerSystem : EntitySystem
{

    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly MessagesCartridgeSystem _messagesCartridgeSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessagesServerComponent, DeviceNetworkPacketEvent>(OnPacketReceived);
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var serverQuery = EntityQueryEnumerator<MessagesServerComponent>();
        while (serverQuery.MoveNext(out var uid, out var server))
        {
            if (!_singletonServerSystem.IsActiveServer(uid))
                continue;
            if (server.NextUpdate <= _gameTiming.CurTime)
            {
                server.NextUpdate += server.UpdateDelay;

                Update(uid, server);
            }
        }
    }

    public void Update(EntityUid uid, MessagesServerComponent component)
    {
        if (!this.IsPowered(uid, EntityManager))
            return;

        var serverQuery = EntityManager.AllEntityQueryEnumerator<MessagesServerComponent>();

        while (serverQuery.MoveNext(out var serverUid, out var serverComponent))
        {
            component.Messages = new List<MessagesMessageData>(component.Messages.Union(serverComponent.Messages));
            serverComponent.Messages = new List<MessagesMessageData>(component.Messages);
        }

        if (!TryComp(uid, out DeviceNetworkComponent? device))
            return;

        var packet = new NetworkPayload()
        {
            ["NameQuery"] = true,
            ["ServerComponent"] = component
        };
        _deviceNetworkSystem.QueuePacket(uid, null, packet, device: device);
    }

    private void OnPacketReceived(EntityUid uid, MessagesServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!_singletonServerSystem.IsActiveServer(uid))
            return;
        if (args.Data.TryGetValue<string>("NewName", out var name) && args.Data.TryGetValue<int>("UserId", out var userId))
            component.NameDict[userId] = name;
        if (args.Data.TryGetValue<MessagesMessageData>("Message", out var message))
            SendMessage(uid, component, message);
    }

    ///<summary>
    ///Function that tries to send a message to any matching cartridges on its map
    ///</summary>
    public void SendMessage(EntityUid uid, MessagesServerComponent component, MessagesMessageData message)
    {
        component.Messages.Add(message);

        if (!TryComp(uid, out DeviceNetworkComponent? device))
            return;

        var packet = new NetworkPayload()
        {
            ["Message"] = message,
            ["ServerComponent"] = component
        };

        _deviceNetworkSystem.QueuePacket(uid, null, packet, device: device);
    }

    public string GetNameFromDict(MessagesServerComponent component, int key)
    {
        if (component.NameDict.TryGetValue(key, out var value))
            return value;
        return Loc.GetString("messages-pda-user-missing");
    }

    public Dictionary<int, string> GetNameDict(MessagesServerComponent component)
    {
        return component.NameDict;
    }

    public List<MessagesMessageData> GetMessages(MessagesServerComponent component, int id1, int id2)
    {
        return new List<MessagesMessageData>(component.Messages.Where(message => message.SenderId == id1 && message.ReceiverId == id2 || message.SenderId == id2 && message.ReceiverId == id1));
    }

}
