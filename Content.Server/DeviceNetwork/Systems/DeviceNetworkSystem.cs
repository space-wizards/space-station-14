using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Content.Server.GameTicking.Events;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.GameTicking;
using Robust.Server.GameStates;

namespace Content.Server.DeviceNetwork.Systems;

/// <summary>
///     Entity system that handles everything device network related.
///     Device networking allows machines and devices to communicate with each other while adhering to restrictions like range or being connected to the same powernet.
/// </summary>
[UsedImplicitly]
public sealed partial class DeviceNetworkSystem : SharedDeviceNetworkSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private DeviceListSystem _deviceLists = default!;
    [Dependency] private NetworkConfiguratorSystem _configurator = default!;
    [Dependency] private PvsOverrideSystem _pvsOverride = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
        SubscribeLocalEvent<DeviceNetworkManagerComponent, MapInitEvent>(OnManagerInit);
        SubscribeLocalEvent<DeviceNetworkComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DeviceNetworkComponent, ComponentShutdown>(OnNetworkShutdown);
    }

    public override void Update(float frameTime)
    {
        if (!TryGetManager(out var manager))
            return;

        var comp = manager.Value.Comp;
        while (comp.ActiveQueue.TryDequeue(out var packet))
        {
            SendPacket(ref packet);
        }

        while (comp.HandledActiveQueue.TryDequeue(out var packet))
        {
            SendPacketHandled(ref packet);
        }

        while (comp.ParallelActiveQueue.TryDequeue(out var packet))
        {
            SendPacketParallel(ref packet);
        }

        SwapQueues(comp);
    }

    /// <summary>
    /// Swaps the active queue.
    /// Queues are swapped so that packets being sent in the current tick get processed in the next tick.
    /// </summary>
    /// <remarks>
    /// This prevents infinite loops while sending packets
    /// </remarks>
    private void SwapQueues(DeviceNetworkManagerComponent manager)
    {
        manager.NextQueue = manager.ActiveQueue;
        manager.ActiveQueue = manager.ActiveQueue == manager.QueueA ? manager.QueueB : manager.QueueA;
        manager.HandledNextQueue = manager.HandledActiveQueue;
        manager.HandledActiveQueue = manager.HandledActiveQueue == manager.QueueC ? manager.QueueD : manager.QueueC;
        manager.ParallelNextQueue = manager.ParallelActiveQueue;
        manager.ParallelActiveQueue = manager.ParallelActiveQueue == manager.QueueE ? manager.QueueF : manager.QueueE;
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        EnsureManager();
    }

    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        ClearManager();
    }

    private void OnManagerInit(Entity<DeviceNetworkManagerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ActiveQueue = ent.Comp.QueueA;
        ent.Comp.NextQueue = ent.Comp.QueueB;
        ent.Comp.HandledActiveQueue = ent.Comp.QueueC;
        ent.Comp.HandledNextQueue = ent.Comp.QueueD;
        ent.Comp.ParallelActiveQueue = ent.Comp.QueueE;
        ent.Comp.ParallelNextQueue = ent.Comp.QueueF;
        _pvsOverride.AddGlobalOverride(ent);
    }

    private Entity<DeviceNetworkManagerComponent> EnsureManager()
    {
        if (TryGetManager(out var found))
            return found.Value;

        var manager = Spawn();
        var managerComp = AddComp<DeviceNetworkManagerComponent>(manager);
        return (manager, managerComp);
    }

    private bool TryGetManager([NotNullWhen(true)] out Entity<DeviceNetworkManagerComponent>? ent)
    {
        ent = null;
        var query = EntityQueryEnumerator<DeviceNetworkManagerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            ent = (uid, comp);
            return true;
        }

        return false;
    }

    private void ClearManager()
    {
        if (TryGetManager(out var found))
            Del(found);
    }

    /// <summary>
    /// Removes the <see cref="DeviceNetworkManagerComponent"/> if it no longer has any entities in its networks.
    /// </summary>
    private void CheckClearManager()
    {
        if (!TryGetManager(out var found))
            return;

        foreach (var network in found.Value.Comp.Networks.Values)
        {
            if (network.Devices.Count != 0)
                return;
        }

        Del(found);
    }

    /// <summary>
    /// Automatically attempt to connect some devices when a map starts.
    /// </summary>
    private void OnMapInit(Entity<DeviceNetworkComponent> ent, ref MapInitEvent args)
    {
        var device = ent.Comp;
        if (device.ReceiveFrequency == null
            && device.ReceiveFrequencyId != null
            && ProtoMan.TryIndex(device.ReceiveFrequencyId, out var receive))
        {
            device.ReceiveFrequency = receive.Frequency;
        }

        if (device.TransmitFrequency == null
            && device.TransmitFrequencyId != null
            && ProtoMan.TryIndex(device.TransmitFrequencyId, out var xmit))
        {
            device.TransmitFrequency = xmit.Frequency;
        }

        // Needed for example for tests, so when there's a device, there's also always a manager that can handle it.
        EnsureManager();

        if (device.AutoConnect)
            ConnectDevice(ent.AsNullable());
    }

    /// <summary>
    /// Automatically disconnect when an entity with a DeviceNetworkComponent shuts down.
    /// </summary>
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

        CheckClearManager();
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
        if (!TryGetManager(out var manager))
            return false;

        if (manager.Value.Comp.Networks.TryGetValue(netId, out var deviceNet))
        {
            network = deviceNet;
            return true;
        }

        var newDeviceNet = new DeviceNet(netId, _random);
        manager.Value.Comp.Networks[netId] = newDeviceNet;
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
        if (!TryGetManager(out var manager))
            return false;

        if (!manager.Value.Comp.Networks.TryGetValue(netId, out var deviceNet))
            return false;

        network = deviceNet;
        return true;
    }

    private void SendPacket(ref DeviceNetworkPacketEvent packet)
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

            if (!device.Value.ReceiveAll &&
                device.Value.ReceiveFrequency == packet.Frequency)
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
    private bool CheckRecipientsList(DeviceNetworkPacketEvent packet, ref HashSet<Device> recipients)
    {
        var manager = EnsureManager();
        if (!manager.Comp.Networks.ContainsKey(packet.NetId) || !manager.Comp.Networks[packet.NetId].Devices.ContainsKey(packet.SenderAddress))
            return false;

        var sender = manager.Comp.Networks[packet.NetId].Devices[packet.SenderAddress];
        if (!sender.SendBroadcastAttemptEvent)
            return true;

        var beforeBroadcastAttemptEvent = new BeforeBroadcastAttemptEvent(recipients);
        RaiseLocalEvent(packet.Sender, ref beforeBroadcastAttemptEvent, true);

        if (beforeBroadcastAttemptEvent.Cancelled || beforeBroadcastAttemptEvent.ModifiedRecipients == null)
            return false;

        recipients = beforeBroadcastAttemptEvent.ModifiedRecipients;
        return true;
    }

    /// <summary>
    /// Sends the <see cref="BeforeBroadcastAttemptEvent"/> to the sending entity if the packets SendBeforeBroadcastAttemptEvent field is set to true.
    /// The recipients is set to the modified recipient list.
    /// </summary>
    /// <returns>false if the broadcast was canceled</returns>
    private bool CheckRecipientsList(DeviceNetworkPacketHandledEvent packet, ref HashSet<Device> recipients)
    {
        var manager = EnsureManager();
        if (!manager.Comp.Networks.ContainsKey(packet.NetId) || !manager.Comp.Networks[packet.NetId].Devices.ContainsKey(packet.SenderAddress))
            return false;

        var sender = manager.Comp.Networks[packet.NetId].Devices[packet.SenderAddress];
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

            var beforeEv = new BeforePacketSentEvent(packet.NetId,
                packet.Address,
                packet.Frequency,
                packet.SenderAddress,
                packet.Sender,
                xform,
                senderPos);
            RaiseLocalEvent(connection.DeviceOwner, ref beforeEv);

            if (beforeEv.Cancelled)
                continue;

            RaiseLocalEvent(connection.DeviceOwner, ref packet);
        }
    }

    private void SendPacketHandled(ref DeviceNetworkPacketHandledEvent packet)
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
                SendToConnectionsHandled(deviceCopy.AsSpan(0, devices.Count), packet);
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

            if (!device.Value.ReceiveAll &&
                device.Value.ReceiveFrequency == packet.Frequency)
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
            SendToConnectionsHandled(deviceCopy.AsSpan(0, totalDevices), packet);
            ArrayPool<Device>.Shared.Return(deviceCopy);
        }
    }

    private void SendToConnectionsHandled(ReadOnlySpan<Device> connections, DeviceNetworkPacketHandledEvent packet)
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

            var beforeEv = new BeforePacketSentEvent(packet.NetId,
                packet.Address,
                packet.Frequency,
                packet.SenderAddress,
                packet.Sender,
                xform,
                senderPos);
            RaiseBeforePayload(connection.DeviceOwner, ref beforeEv);
            if (beforeEv.Cancelled)
                continue;

            var data = new DeviceNetworkPacketData(
                packet.NetId,
                packet.Address,
                packet.Frequency,
                packet.SenderAddress,
                packet.Sender,
                xform,
                senderPos);
            var handledNetworkPayload = packet.Data;
            RaisePayload(connection.DeviceOwner, ref handledNetworkPayload, ref data);
        }
    }

    private void SendPacketParallel(ref DeviceNetworkPacketHandledEvent packet)
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
                SendToConnectionsParallel(deviceCopy.AsSpan(0, devices.Count), packet);
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

            if (!device.Value.ReceiveAll &&
                device.Value.ReceiveFrequency == packet.Frequency)
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
            SendToConnectionsHandled(deviceCopy.AsSpan(0, totalDevices), packet);
            ArrayPool<Device>.Shared.Return(deviceCopy);
        }
    }

    private void SendToConnectionsParallel(ReadOnlySpan<Device> connections, DeviceNetworkPacketHandledEvent packet)
    {
        if (Deleted(packet.Sender))
        {
            return;
        }

        var xform = Transform(packet.Sender);
        var senderPos = _transformSystem.GetWorldPosition(xform);

        Span<EntityUid?> ents = stackalloc EntityUid?[connections.Length];
        for (int i = 0; i < connections.Length; i++)
        {
            ents[i] = null;

            var beforeEv = new BeforePacketSentEvent(packet.NetId,
                packet.Address,
                packet.Frequency,
                packet.SenderAddress,
                packet.Sender,
                xform,
                senderPos);
            RaiseBeforePayload(connections[i].DeviceOwner, ref beforeEv);
            if (beforeEv.Cancelled)
                continue;

            ents[i] = connections[i].DeviceOwner;
        }

        var data = new DeviceNetworkPacketData(
            packet.NetId,
            packet.Address,
            packet.Frequency,
            packet.SenderAddress,
            packet.Sender,
            xform,
            senderPos);
        var handledNetworkPayload = packet.Data;
        RaisePayloadParallel(ents, ref handledNetworkPayload, ref data);
    }
}
