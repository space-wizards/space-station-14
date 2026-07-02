using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using JetBrains.Annotations;

namespace Content.Server.DeviceNetwork.Systems;

public sealed partial class DeviceNetworkSystem
{
    [PublicAPI]
    public override bool QueuePacket(
        Entity<DeviceNetworkComponent?> ent,
        string? address,
        NetworkPayload data,
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
        manager.Comp.NextQueue.Enqueue(new DeviceNetworkPacketEvent(network.Value, address, frequency.Value, device.Address, ent, data));
        return true;
    }

    [PublicAPI]
    public override bool QueuePacket(
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
    public override bool QueuePacketParallel(
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
        Dirty(ent);
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

        var result = deviceNet.Remove(ent!);
        CheckClearManager();
        return result;
    }

    /// <summary>
    /// Checks if a device is already connected to its network
    /// </summary>
    /// <returns>True if the device was found in the network with its corresponding network id</returns>
    [PublicAPI]
    public bool IsDeviceConnected(Entity<DeviceNetworkComponent?> ent)
    {
        var (uid, deviceComp) = ent;
        if (!Resolve(uid, ref deviceComp, false))
            return false;

        if (!TryGetManager(out var manager)
            || !manager.Value.Comp.Networks.TryGetValue(deviceComp.DeviceNetId, out var deviceNet))
            return false;

        var device = new Device((uid, deviceComp));
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
        Dirty(ent);
    }

    [PublicAPI]
    public void SetTransmitFrequency(Entity<DeviceNetworkComponent?> ent, uint? frequency)
    {
        if (Resolve(ent.Owner, ref ent.Comp, false))
            ent.Comp.TransmitFrequency = frequency;
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
        Dirty(ent);
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
        Dirty(ent);
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
        Dirty(ent);
    }
}
