using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.Random;

namespace Content.Server.DeviceNetwork.Systems;

/// <inheritdoc/>
public sealed partial class DeviceNetworkSystem : SharedDeviceNetworkSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private DeviceListSystem _deviceLists = default!;
    [Dependency] private NetworkConfiguratorSystem _configurator = default!;

    /// <summary>
    /// Basically a cache of devices to connect them together faster.
    /// </summary>
    private readonly Dictionary<int, DeviceNet> _networks = new(4);

    /// <summary>
    /// Automatically attempt to connect some devices when a map starts.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<DeviceNetworkComponent> ent, ref MapInitEvent args)
    {
        var device = ent.Comp;
        if (device.Data.ReceiveFrequency == null
            && device.ReceiveFrequencyId != null
            && ProtoMan.TryIndex(device.ReceiveFrequencyId, out var receive))
        {
            device.Data.ReceiveFrequency = receive.Frequency;
        }

        if (device.Data.TransmitFrequency == null
            && device.TransmitFrequencyId != null
            && ProtoMan.TryIndex(device.TransmitFrequencyId, out var xmit))
        {
            device.Data.TransmitFrequency = xmit.Frequency;
        }

        if (device.AutoConnect)
            ConnectDevice(ent.AsNullable());
    }

    /// <summary>
    /// Automatically disconnect when an entity with a DeviceNetworkComponent shuts down.
    /// </summary>
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

            if (!TryGetDevice(packet.NetId, packet.Address, out var device))
                return;

            if (!device.Value.DeviceData.ReceiveAll &&
                device.Value.DeviceData.ReceiveFrequency == packet.Frequency)
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
                deviceCopy[totalDevices - 1] = device.Value;
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
}
