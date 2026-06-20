using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using JetBrains.Annotations;

namespace Content.Shared.DeviceNetwork.Systems;

public abstract partial class SharedDeviceNetworkSystem
{
    /// <summary>
    /// Sends the given payload as a device network packet to the entity with the given address and frequency.
    /// Addresses are given to the DeviceNetworkComponent of an entity when connecting.
    /// </summary>
    /// <param name="ent">The sending entity</param>
    /// <param name="address">The address of the entity that the packet gets sent to. If null, the message is broadcast to all devices on that frequency (except the sender)</param>
    /// <param name="frequency">The frequency to send on</param>
    /// <param name="data">The data to be sent</param>
    /// <param name="network">Device network override</param>
    /// <returns>Returns true when the packet was successfully enqueued.</returns>
    [PublicAPI]
    public bool QueuePacket(
        Entity<DeviceNetworkComponent?> ent,
        string? address,
        NetworkPayload data,
        uint? frequency = null,
        int? network = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (ent.Comp.Address == string.Empty)
            return false;

        frequency ??= ent.Comp.TransmitFrequency;

        if (frequency == null)
            return false;

        network ??= ent.Comp.DeviceNetId;

        var manager = EnsureManager();
        manager.Comp.NextQueue.Enqueue(new DeviceNetworkPacketEvent(network.Value, address, frequency.Value, ent.Comp.Address, ent, data));
        return true;
    }

    [PublicAPI]
    public bool QueuePacketHandled(
        Entity<DeviceNetworkComponent?> ent,
        string? address,
        HandledNetworkPayload data,
        uint? frequency = null,
        int? network = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        var device = ent.Comp;
        if (device.Address == string.Empty)
            return false;

        frequency ??= device.TransmitFrequency;

        if (frequency == null)
            return false;

        network ??= device.DeviceNetId;

        var manager = EnsureManager();
        manager.Comp.HandledNextQueue.Enqueue(new DeviceNetworkPacketHandledEvent(network.Value, address, frequency.Value, device.Address, ent, data));
        return true;
    }

    [PublicAPI]
    public bool QueuePacketParallel(
        Entity<DeviceNetworkComponent?> ent,
        string? address,
        HandledNetworkPayload data,
        uint? frequency = null,
        int? network = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        var device = ent.Comp;
        if (device.Address == string.Empty)
            return false;

        frequency ??= device.TransmitFrequency;

        if (frequency == null)
            return false;

        network ??= device.DeviceNetId;

        var manager = EnsureManager();
        manager.Comp.ParallelNextQueue.Enqueue(new DeviceNetworkPacketHandledEvent(network.Value, address, frequency.Value, device.Address, ent, data));
        return true;
    }

    /// <summary>
    /// Connect an entity with a DeviceNetworkComponent. Note that this will re-use an existing address if the
    /// device already had one configured. If there is a clash, the device cannot join the network.
    /// </summary>
    [PublicAPI]
    public bool ConnectDevice(Entity<DeviceNetworkComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (!TryEnsureNetwork(ent.Comp.DeviceNetId, out var deviceNet))
            return false;

        var success = deviceNet.Add(ent!);
        DirtyField(ent, nameof(DeviceNetworkComponent.Address));
        return success;
    }

    /// <summary>
    /// Disconnect an entity with a DeviceNetworkComponent.
    /// </summary>
    [PublicAPI]
    public bool DisconnectDevice(Entity<DeviceNetworkComponent?> ent, bool preventAutoConnect = true)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (!TryGetNetwork(ent.Comp.DeviceNetId, out var deviceNet))
            return false;

        // If manually disconnected, don't auto reconnect when a game state is loaded.
        if (preventAutoConnect)
            ent.Comp.AutoConnect = false;

        return deviceNet.Remove(ent!);
    }

    /// <summary>
    /// Checks if a device is already connected to its network
    /// </summary>
    /// <returns>True if the device was found in the network with its corresponding network id</returns>
    [PublicAPI]
    public bool IsDeviceConnected(Entity<DeviceNetworkComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (!TryGetManager(out var manager)
            || !manager.Value.Comp.Networks.TryGetValue(ent.Comp.DeviceNetId, out var deviceNet))
            return false;

        var device = new Device(ent!);
        return deviceNet.Devices.ContainsValue(device);
    }

    /// <summary>
    /// Checks if an address exists in the network with the given netId
    /// </summary>
    [PublicAPI]
    public bool IsAddressPresent(int netId, string? address)
    {
        if (address == null
            || !TryGetManager(out var manager)
            || !manager.Value.Comp.Networks.TryGetValue(netId, out var network))
            return false;

        return network.Devices.ContainsKey(address);
    }

    [PublicAPI]
    public void SetReceiveFrequency(Entity<DeviceNetworkComponent?> ent, uint? frequency)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (ent.Comp.ReceiveFrequency == frequency)
            return;

        if (!TryGetNetwork(ent.Comp.DeviceNetId, out var deviceNet))
            return;

        deviceNet.Remove(ent!);
        ent.Comp.ReceiveFrequency = frequency;
        deviceNet.Add(ent!);
        DirtyFields(ent, null, nameof(DeviceNetworkComponent.Address), nameof(DeviceNetworkComponent.ReceiveFrequency));
    }

    [PublicAPI]
    public void SetTransmitFrequency(Entity<DeviceNetworkComponent?> ent, uint? frequency)
    {
        if (Resolve(ent.Owner, ref ent.Comp, false))
            ent.Comp.TransmitFrequency = frequency;

        DirtyFields(ent, null, nameof(DeviceNetworkComponent.TransmitFrequency));
    }

    [PublicAPI]
    public void SetReceiveAll(Entity<DeviceNetworkComponent?> ent, bool receiveAll)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (ent.Comp.ReceiveAll == receiveAll)
            return;

        if (!TryGetNetwork(ent.Comp.DeviceNetId, out var deviceNet))
            return;

        deviceNet.Remove(ent!);
        ent.Comp.ReceiveAll = receiveAll;
        deviceNet.Add(ent!);
        DirtyFields(ent, null, nameof(DeviceNetworkComponent.ReceiveAll));
    }

    [PublicAPI]
    public void SetAddress(Entity<DeviceNetworkComponent?> ent, string address)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (ent.Comp.Address == address && ent.Comp.CustomAddress)
            return;

        if (!TryGetNetwork(ent.Comp.DeviceNetId, out var deviceNet))
            return;

        deviceNet.Remove(ent!);
        ent.Comp.CustomAddress = true;
        ent.Comp.Address = address;
        deviceNet.Add(ent!);
        DirtyFields(ent, null, nameof(DeviceNetworkComponent.Address), nameof(DeviceNetworkComponent.CustomAddress));
    }

    [PublicAPI]
    public void RandomizeAddress(Entity<DeviceNetworkComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!TryGetNetwork(ent.Comp.DeviceNetId, out var deviceNet))
            return;

        deviceNet.Remove(ent!);
        ent.Comp.CustomAddress = false;
        ent.Comp.Address = "";
        deviceNet.Add(ent!);
        DirtyFields(ent, null, nameof(DeviceNetworkComponent.Address), nameof(DeviceNetworkComponent.CustomAddress));
    }
}
