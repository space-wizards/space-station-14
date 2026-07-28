using System.Diagnostics.CodeAnalysis;
using Content.Shared.Buffers;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Examine;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.DeviceNetwork.Systems;

/// <summary>
///     Entity system that handles everything device network related.
///     Device networking allows machines and devices to communicate with each other
///     while adhering to restrictions like range or being connected to the same power network.
/// </summary>
public abstract partial class SharedDeviceNetworkSystem : EntitySystem, IDevicePayloadRaiser
{
    [Dependency] private IPrototypeManager _protoMan = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private DeviceListSystem _deviceLists = default!;
    [Dependency] private NetworkConfiguratorSystem _configurator = default!;

    protected BaseContentArrayPool<Device> DeviceArrayPool = default!;
    protected BaseContentArrayPool<EntityUid?> EntityArrayPool = default!;

    // Basically a cache of devices to connect them together faster.
    // TODO make DeviceNets smarter and make them entities
    private readonly Dictionary<int, DeviceNet> _networks = new(4);

    [SubscribeLocalEvent]
    private void OnExamine(Entity<DeviceNetworkComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExaminableAddress)
            args.PushText(Loc.GetString("device-address-examine-message", ("address", ent.Comp.Data.Address)));
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<DeviceNetworkComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.AutoConnect)
            ConnectDevice(ent.AsNullable());
    }

    /// <summary>
    /// Automatically attempt to connect some devices when a map starts.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<DeviceNetworkComponent> ent, ref MapInitEvent args)
    {
        var device = ent.Comp;
        if (device.Data.ReceiveFrequency == null
            && device.ReceiveFrequencyId != null
            && _protoMan.TryIndex(device.ReceiveFrequencyId, out var receive))
        {
            device.Data.ReceiveFrequency = receive.Frequency;
        }

        if (device.Data.TransmitFrequency == null
            && device.TransmitFrequencyId != null
            && _protoMan.TryIndex(device.TransmitFrequencyId, out var xmit))
        {
            device.Data.TransmitFrequency = xmit.Frequency;
        }

        if (ent.Comp.AutoConnect)
            ConnectDevice(ent.AsNullable());

        DirtyField(ent.AsNullable(), nameof(DeviceNetworkComponent.Data));
    }

    [SubscribeLocalEvent]
    private void OnNetworkShutdown(Entity<DeviceNetworkComponent> ent, ref ComponentShutdown args)
    {
        var component = ent.Comp;
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

        if (TryGetNetwork(component.DeviceNetId, out var network))
            network.Remove(ent);
    }

    /// <summary>
    ///     Try to find a device on a network using its address.
    /// </summary>
    private bool TryGetDevice(int netId, string address, [NotNullWhen(true)] out Device? device)
    {
        device = null;
        if (!TryGetNetwork(netId, out var network)
            || !network.Devices.TryGetValue(address, out var foundDevice))
            return false;

        device = foundDevice;
        return true;
    }

    /// <summary>
    /// Tries to get an already existing device network, and creates a new network if it doesn't exist.
    /// </summary>
    /// <returns>False if the manager is not initialized.</returns>
    /// <returns></returns>
    private bool TryEnsureNetwork(int netId, [NotNullWhen(true)] out DeviceNet? network)
    {
        network = null;

        if (_networks.TryGetValue(netId, out var deviceNet))
        {
            network = deviceNet;
            return true;
        }

        var newDeviceNet = new DeviceNet(netId, _random);
        _networks[netId] = newDeviceNet;
        network = newDeviceNet;
        return true;
    }

    /// <summary>
    /// Tries to get an already existing network.
    /// </summary>
    /// <returns>False if the manager is not initialized, or the network wasn't found.</returns>
    private bool TryGetNetwork(int netId, [NotNullWhen(true)] out DeviceNet? network)
    {
        network = null;

        if (!_networks.TryGetValue(netId, out var deviceNet))
            return false;

        network = deviceNet;
        return true;
    }

    private void SendPacket(ref DeviceNetworkPacketData packet)
    {
        if (!TryEnsureNetwork(packet.NetId, out var network))
            return;

        if (packet.Address == null)
        {
            // Broadcast to all listening devices
            if (network.ListeningDevices.TryGetValue(packet.Frequency, out var devices) && CheckRecipientsList(packet, ref devices))
            {
                var deviceCopy = DeviceArrayPool.Rent(devices.Count);
                devices.CopyTo(deviceCopy);
                SendToConnections(deviceCopy.AsSpan(0, devices.Count), packet);
                DeviceArrayPool.Return(deviceCopy);
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

            if (!TryGetDevice(packet.NetId, packet.Address, out var device))
                return;

            if (!device.Value.DeviceData.ReceiveAll &&
                device.Value.DeviceData.ReceiveFrequency == packet.Frequency)
            {
                totalDevices += 1;
                hasTargetedDevice = true;
            }
            var deviceCopy = DeviceArrayPool.Rent(totalDevices);
            if (devices != null)
            {
                devices.CopyTo(deviceCopy);
            }
            if (hasTargetedDevice)
            {
                deviceCopy[totalDevices - 1] = device.Value;
            }
            SendToConnections(deviceCopy.AsSpan(0, totalDevices), packet);
            DeviceArrayPool.Return(deviceCopy);
        }
    }

    /// <summary>
    /// Sends the <see cref="BeforeBroadcastAttemptEvent"/> to the sending entity if the packets SendBeforeBroadcastAttemptEvent field is set to true.
    /// The recipients is set to the modified recipient list.
    /// </summary>
    /// <returns>false if the broadcast was canceled</returns>
    private bool CheckRecipientsList(DeviceNetworkPacketData packet, ref HashSet<Device> recipients)
    {
        if (!_networks.TryGetValue(packet.NetId, out var net)
            || !net.Devices.TryGetValue(packet.SenderAddress, out var device))
            return false;

        var senderData = device.DeviceData;
        if (!senderData.SendBroadcastAttemptEvent)
            return true;

        var beforeBroadcastAttemptEvent = new BeforeBroadcastAttemptEvent(recipients);
        RaiseLocalEvent(packet.Sender, ref beforeBroadcastAttemptEvent, true);

        if (beforeBroadcastAttemptEvent.Cancelled || beforeBroadcastAttemptEvent.ModifiedRecipients == null)
            return false;

        recipients = beforeBroadcastAttemptEvent.ModifiedRecipients;
        return true;
    }

    private void SendToConnections(ReadOnlySpan<Device> connections, DeviceNetworkPacketData packet)
    {
        if (Deleted(packet.Sender))
        {
            return;
        }

        var xform = Transform(packet.Sender);

        var senderPos = _transformSystem.GetWorldPosition(xform);

        foreach (var connection in connections)
        {
            if (connection.Owner == packet.Sender)
                continue;

            var beforeEv = new BeforePacketSentEvent(packet.NetId,
                packet.Address,
                packet.Frequency,
                packet.SenderAddress,
                packet.Sender,
                xform,
                senderPos);
            RaiseLocalEvent(connection.Owner, ref beforeEv);

            if (beforeEv.Cancelled)
                continue;

            packet.Data.RaiseEvent(connection.Owner, this, ref packet);
        }
    }

    /// <summary>
    /// Raises a device network packet to an entity. You should not be calling this unless you know what you're doing.
    /// </summary>
    public void RaisePayloadEvent<T>(EntityUid target, T payload, ref DeviceNetworkPacketData packet) where T : NetworkPayloadBase<T>
    {
        var ev = new DeviceNetworkPacketEvent<T>(
            packet.NetId,
            packet.Address,
            packet.Frequency,
            packet.SenderAddress,
            packet.Sender,
            payload);
        RaiseLocalEvent(target, ref ev);
    }
}

/// <summary>
/// Used to raise an <see cref="NetworkPayload"/> without losing the type of effect.
/// </summary>
public interface IDevicePayloadRaiser
{
    void RaisePayloadEvent<T>(EntityUid target, T payload, ref DeviceNetworkPacketData packet) where T : NetworkPayloadBase<T>;
}
