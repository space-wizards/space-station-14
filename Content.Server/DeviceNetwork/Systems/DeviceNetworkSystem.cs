using Content.Shared.DeviceNetwork;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Buffers;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Examine;

namespace Content.Server.DeviceNetwork.Systems;

/// <summary>
///     Entity system that handles everything device network related.
///     Device networking allows machines and devices to communicate with each other while adhering to restrictions like range or being connected to the same powernet.
/// </summary>
[UsedImplicitly]
public sealed partial class DeviceNetworkSystem : SharedDeviceNetworkSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly DeviceListSystem _deviceLists = default!;
    [Dependency] private NetworkConfiguratorSystem _configurator = default!;

    private readonly Dictionary<int, DeviceNet> _networks = new(4);
    private readonly Queue<DeviceNetworkPacketEvent> _queueA = new();
    private readonly Queue<DeviceNetworkPacketEvent> _queueB = new();

    /// <summary>
    /// The queue being processed in the current tick
    /// </summary>
    private Queue<DeviceNetworkPacketEvent> _activeQueue = null!;

    /// <summary>
    /// The queue that will be processed in the next tick
    /// </summary>
    private Queue<DeviceNetworkPacketEvent> _nextQueue = null!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DeviceNetworkComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DeviceNetworkComponent, ComponentShutdown>(OnNetworkShutdown);
        SubscribeLocalEvent<DeviceNetworkComponent, ExaminedEvent>(OnExamine);

        _activeQueue = _queueA;
        _nextQueue = _queueB;
    }

    public override void Update(float frameTime)
    {
        while (_activeQueue.TryDequeue(out var packet))
        {
            SendPacket(ref packet);
        }

        SwapQueues();
    }

    public override bool QueuePacket(EntityUid uid, string? address, NetworkPayload data, uint? frequency = null, int? network = null, DeviceNetworkComponent? device = null)
    {
        if (!Resolve(uid, ref device, false))
            return false;

        if (device.Address == string.Empty)
            return false;

        frequency ??= device.TransmitFrequency;

        if (frequency == null)
            return false;

        network ??= device.DeviceNetId;

        _nextQueue.Enqueue(new DeviceNetworkPacketEvent(network.Value, address, frequency.Value, device.Address, uid, data));
        return true;
    }

    /// <summary>
    /// Swaps the active queue.
    /// Queues are swapped so that packets being sent in the current tick get processed in the next tick.
    /// </summary>
    /// <remarks>
    /// This prevents infinite loops while sending packets
    /// </remarks>
    private void SwapQueues()
    {
        _nextQueue = _activeQueue;
        _activeQueue = _activeQueue == _queueA ? _queueB : _queueA;
    }

    private void OnExamine(Entity<DeviceNetworkComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExaminableAddress)
        {
            args.PushText(Loc.GetString("device-address-examine-message", ("address", ent.Comp.Address)));
        }
    }

    /// <summary>
    /// Automatically attempt to connect some devices when a map starts.
    /// </summary>
    private void OnMapInit(Entity<DeviceNetworkComponent> ent, ref MapInitEvent args)
    {
        var device = ent.Comp;
        if (device.ReceiveFrequency == null
            && device.ReceiveFrequencyId != null
            && _protoMan.TryIndex<DeviceFrequencyPrototype>(device.ReceiveFrequencyId, out var receive))
        {
            device.ReceiveFrequency = receive.Frequency;
        }

        if (device.TransmitFrequency == null
            && device.TransmitFrequencyId != null
            && _protoMan.TryIndex<DeviceFrequencyPrototype>(device.TransmitFrequencyId, out var xmit))
        {
            device.TransmitFrequency = xmit.Frequency;
        }

        if (device.AutoConnect)
            ConnectDevice(ent.AsNullable());
    }

    private DeviceNet GetNetwork(int netId)
    {
        if (_networks.TryGetValue(netId, out var deviceNet))
            return deviceNet;
        var newDeviceNet = new DeviceNet(netId, _random);
        _networks[netId] = newDeviceNet;
        return newDeviceNet;
    }

    /// <summary>
    /// Automatically disconnect when an entity with a DeviceNetworkComponent shuts down.
    /// </summary>
    private void OnNetworkShutdown(Entity<DeviceNetworkComponent> ent, ref ComponentShutdown args)
    {
        var (uid, component) = ent;
        foreach (var list in component.DeviceLists)
        {
            if (Deleted(list))
                return;

            _deviceLists.OnDeviceShutdown(list, ent);
        }

        foreach (var list in component.Configurators)
        {
            if (Deleted(list))
                return;

            _configurator.OnDeviceShutdown(list, ent);
        }

        GetNetwork(component.DeviceNetId).Remove(ent);
    }

    /// <summary>
    /// Connect an entity with a DeviceNetworkComponent. Note that this will re-use an existing address if the
    /// device already had one configured. If there is a clash, the device cannot join the network.
    /// </summary>
    public bool ConnectDevice(Entity<DeviceNetworkComponent?> ent)
    {
        var (uid, deviceComp) = ent;
        if (!Resolve(uid, ref deviceComp, false))
            return false;

        return GetNetwork(deviceComp.DeviceNetId).Add((uid, deviceComp));
    }

    /// <summary>
    /// Disconnect an entity with a DeviceNetworkComponent.
    /// </summary>
    public bool DisconnectDevice(Entity<DeviceNetworkComponent?> ent, bool preventAutoConnect = true)
    {
        var (uid, deviceComp) = ent;
        if (!Resolve(uid, ref deviceComp, false))
            return false;

        // If manually disconnected, don't auto reconnect when a game state is loaded.
        if (preventAutoConnect)
            deviceComp.AutoConnect = false;

        return GetNetwork(deviceComp.DeviceNetId).Remove((uid, deviceComp));
    }

    /// <summary>
    /// Checks if a device is already connected to its network
    /// </summary>
    /// <returns>True if the device was found in the network with its corresponding network id</returns>
    public bool IsDeviceConnected(Entity<DeviceNetworkComponent?> ent)
    {
        var (uid, deviceComp) = ent;
        if (!Resolve(uid, ref deviceComp, false))
            return false;

        if (!_networks.TryGetValue(deviceComp.DeviceNetId, out var deviceNet))
            return false;

        var device = new Device((uid, deviceComp));
        return deviceNet.Devices.ContainsValue(device);
    }

    /// <summary>
    /// Checks if an address exists in the network with the given netId
    /// </summary>
    public bool IsAddressPresent(int netId, string? address)
    {
        if (address == null || !_networks.TryGetValue(netId, out var network))
            return false;

        return network.Devices.ContainsKey(address);
    }

    public void SetReceiveFrequency(Entity<DeviceNetworkComponent?> ent, uint? frequency)
    {
        var (uid, deviceComp) = ent;
        if (!Resolve(uid, ref deviceComp, false))
            return;

        if (deviceComp.ReceiveFrequency == frequency)
            return;

        var deviceNet = GetNetwork(deviceComp.DeviceNetId);
        deviceNet.Remove((uid, deviceComp));
        deviceComp.ReceiveFrequency = frequency;
        deviceNet.Add((uid, deviceComp));
    }

    public void SetTransmitFrequency(EntityUid uid, uint? frequency, DeviceNetworkComponent? device = null)
    {
        if (Resolve(uid, ref device, false))
            device.TransmitFrequency = frequency;
    }

    public void SetReceiveAll(Entity<DeviceNetworkComponent?> ent, bool receiveAll)
    {
        var (uid, deviceComp) = ent;
        if (!Resolve(uid, ref deviceComp, false))
            return;

        if (deviceComp.ReceiveAll == receiveAll)
            return;

        var deviceNet = GetNetwork(deviceComp.DeviceNetId);
        deviceNet.Remove((uid, deviceComp));
        deviceComp.ReceiveAll = receiveAll;
        deviceNet.Add((uid, deviceComp));
    }

    public void SetAddress(Entity<DeviceNetworkComponent?> ent, string address)
    {
        var (uid, deviceComp) = ent;
        if (!Resolve(uid, ref deviceComp, false))
            return;

        if (deviceComp.Address == address && deviceComp.CustomAddress)
            return;

        var deviceNet = GetNetwork(deviceComp.DeviceNetId);

        deviceNet.Remove((uid, deviceComp));
        deviceComp.CustomAddress = true;
        deviceComp.Address = address;
        deviceNet.Add((uid, deviceComp));
    }

    public void RandomizeAddress(Entity<DeviceNetworkComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;
        var deviceNet = GetNetwork(ent.Comp.DeviceNetId);
        deviceNet.Remove((ent.Owner, ent.Comp));
        ent.Comp.CustomAddress = false;
        ent.Comp.Address = "";
        deviceNet.Add((ent.Owner, ent.Comp));
    }

    /// <summary>
    ///     Try to find a device on a network using its address.
    /// </summary>
    private bool TryGetDevice(int netId, string address, out Device device) =>
        GetNetwork(netId).Devices.TryGetValue(address, out device);

    private void SendPacket(ref DeviceNetworkPacketEvent packet)
    {
        var network = GetNetwork(packet.NetId);
        if (packet.Address == null)
        {
            // Broadcast to all listening devices
            if (network.ListeningDevices.TryGetValue(packet.Frequency, out var devices) && CheckRecipientsList(packet, ref devices))
            {
                var deviceCopy = ArrayPool<Device>.Shared.Rent(devices.Count);
                devices.CopyTo(deviceCopy);
                SendToConnections(deviceCopy.AsSpan(0, devices.Count), packet);
                ArrayPool<Device>.Shared.Return(deviceCopy);
            }
        }
        else
        {
            var totalDevices = 0;
            var hasTargetedDevice = false;
            if (network.ReceiveAllDevices.TryGetValue(packet.Frequency, out var devices))
            {
                totalDevices += devices.Count;
            }
            if (TryGetDevice(packet.NetId, packet.Address, out var device) &&
                !device.ReceiveAll &&
                device.ReceiveFrequency == packet.Frequency)
            {
                totalDevices += 1;
                hasTargetedDevice = true;
            }
            var deviceCopy = ArrayPool<Device>.Shared.Rent(totalDevices);
            if (devices != null)
            {
                devices.CopyTo(deviceCopy);
            }
            if (hasTargetedDevice)
            {
                deviceCopy[totalDevices - 1] = device!;
            }
            SendToConnections(deviceCopy.AsSpan(0, totalDevices), packet);
            ArrayPool<Device>.Shared.Return(deviceCopy);
        }
    }

    /// <summary>
    /// Sends the <see cref="BeforeBroadcastAttemptEvent"/> to the sending entity if the packets SendBeforeBroadcastAttemptEvent field is set to true.
    /// The recipients is set to the modified recipient list.
    /// </summary>
    /// <returns>false if the broadcast was canceled</returns>
    private bool CheckRecipientsList(DeviceNetworkPacketEvent packet, ref HashSet<Device> recipients)
    {
        if (!_networks.ContainsKey(packet.NetId) || !_networks[packet.NetId].Devices.ContainsKey(packet.SenderAddress))
            return false;

        var sender = _networks[packet.NetId].Devices[packet.SenderAddress];
        if (!sender.SendBroadcastAttemptEvent)
            return true;

        var beforeBroadcastAttemptEvent = new BeforeBroadcastAttemptEvent(recipients);
        RaiseLocalEvent(packet.Sender, ref beforeBroadcastAttemptEvent, true);

        if (beforeBroadcastAttemptEvent.Cancelled || beforeBroadcastAttemptEvent.ModifiedRecipients == null)
            return false;

        recipients = beforeBroadcastAttemptEvent.ModifiedRecipients;
        return true;
    }

    private void SendToConnections(ReadOnlySpan<Device> connections, DeviceNetworkPacketEvent packet)
    {
        if (Deleted(packet.Sender))
        {
            return;
        }

        var xform = Transform(packet.Sender);

        var senderPos = _transformSystem.GetWorldPosition(xform);

        foreach (var connection in connections)
        {
            if (connection.DeviceOwner == packet.Sender)
                continue;

            var beforeEv = new BeforePacketSentEvent(packet.Sender, xform, senderPos, connection.NetIdEnum.ToString());
            RaiseLocalEvent(connection.DeviceOwner, ref beforeEv);

            if (!beforeEv.Cancelled)
                RaiseLocalEvent(connection.DeviceOwner, ref packet);
            else
                beforeEv.Cancelled = false;
        }
    }
}
