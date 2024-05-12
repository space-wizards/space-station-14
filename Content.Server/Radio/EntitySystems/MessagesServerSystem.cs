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

    /// <summary>
    /// Reacts to packets received from clients
    /// </summary>
    private void OnPacketReceived(EntityUid uid, MessagesServerComponent component, DeviceNetworkPacketEvent args)
    {
        if (!_singletonServerSystem.IsActiveServer(uid))
            return;
        if (args.Data.TryGetValue<string>(MessagesNetworkKeys.NewName, out var name) && args.Data.TryGetValue<int>(MessagesNetworkKeys.UserId, out var userId))
        {
            component.NameDict[userId] = name;

            var packet = new NetworkPayload();
            _deviceNetworkSystem.QueuePacket(uid, args.SenderAddress, packet);
        }
        if (args.Data.TryGetValue<MessagesMessageData>(MessagesNetworkKeys.Message, out var message))
            SendMessage(uid, component, message);
    }

    /// <summary>
    /// Broadcasts a message into the network
    /// </summary>
    public void SendMessage(EntityUid uid, MessagesServerComponent component, MessagesMessageData message)
    {
        component.Messages.Add(message);

        var packet = new NetworkPayload()
        {
            [MessagesNetworkKeys.Message] = message
        };

        _deviceNetworkSystem.QueuePacket(uid, null, packet);
    }

    /// <summary>
    /// Returns the name of a given user
    /// </summary>
    public bool TryGetNameFromDict(EntityUid? uid, int key, out string name)
    {
        if (!TryComp(uid, out MessagesServerComponent? component))
        {
            name = Loc.GetString("messages-pda-connection-error");
            return false;
        }
        if (component.NameDict.TryGetValue(key, out var keyValue))
        {
            name = keyValue;
            return true;
        }
        name = Loc.GetString("messages-pda-user-missing");
        return false;
    }

    /// <summary>
    /// Returns the name dictionary cache
    /// </summary>
    public Dictionary<int, string> GetNameDict(EntityUid? uid)
    {
        if (!TryComp(uid, out MessagesServerComponent? component))
            return new Dictionary<int, string>();
        return component.NameDict;
    }

    /// <summary>
    /// Returns list of messages between the two users
    /// </summary>
    public List<MessagesMessageData> GetMessages(EntityUid? uid, int id1, int id2)
    {
        if (!TryComp(uid, out MessagesServerComponent? component))
            return new List<MessagesMessageData>();
        return new List<MessagesMessageData>(component.Messages.Where(message => message.SenderId == id1 && message.ReceiverId == id2 || message.SenderId == id2 && message.ReceiverId == id1));
    }

}

public  enum MessagesNetworkKeys : string
{
    NewName,
    UserId,
    Message
};

