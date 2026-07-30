using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using JetBrains.Annotations;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class DeviceNetworkSystem
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
    public bool QueuePacket<T>(
        Entity<DeviceNetworkComponent?> ent,
        string? address,
        ref T data,
        uint? frequency = null,
        int? network = null)
        where T : INetworkPayload
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        var device = ent.Comp;
        if (device.Data.Address == string.Empty)
            return false;

        frequency ??= device.Data.TransmitFrequency;

        if (frequency == null)
            return false;

        network ??= ent.Comp.DeviceNetId;

        var packet = new DeviceNetworkPacketEvent<T>(network.Value, address, frequency.Value, device.Data.Address, ent.Owner, data);
        SendPacket(ref packet);
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
        DirtyField(ent, nameof(DeviceNetworkComponent.Data));
        return success;
    }

    /// <summary>
    /// Disconnect an entity with a DeviceNetworkComponent.
    /// </summary>
    /// <param name="ent">The entity to disconnect from its network.</param>
    /// <param name="preventAutoConnect">
    /// If true, sets <see cref="DeviceNetworkComponent.AutoConnect"/> to false.
    /// That way the device doesn't auto reconnect when a game state is loaded.
    /// </param>
    /// <returns>True if the device was removed from the network successfully.</returns>
    [PublicAPI]
    public bool DisconnectDevice(Entity<DeviceNetworkComponent?> ent, bool preventAutoConnect = true)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (!TryGetNetwork(ent.Comp.DeviceNetId, out var deviceNet))
            return false;

        if (preventAutoConnect)
            ent.Comp.AutoConnect = false;

        return deviceNet.Remove(ent!);
    }

    /// <summary>
    /// Checks if a device is already connected to its network.
    /// </summary>
    /// <returns>True if the device was found in the network with its corresponding network id.</returns>
    [PublicAPI]
    public bool IsDeviceConnected(Entity<DeviceNetworkComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (!_networks.TryGetValue(ent.Comp.DeviceNetId, out var deviceNet))
            return false;

        var device = new Device(ent.Owner, ent.Comp.Data);
        return deviceNet.Devices.ContainsValue(device);
    }

    /// <summary>
    /// Checks if an address exists in the network with the given netId.
    /// </summary>
    [PublicAPI]
    public bool IsAddressPresent(int netId, string? address)
    {
        if (address == null
            || !_networks.TryGetValue(netId, out var network))
            return false;

        return network.Devices.ContainsKey(address);
    }

    /// <summary>
    /// Sets the receive frequency of an entity.
    /// </summary>
    /// <param name="ent">The target device.</param>
    /// <param name="frequency">The new frequency.</param>
    [PublicAPI]
    public void SetReceiveFrequency(Entity<DeviceNetworkComponent?> ent, uint? frequency)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (ent.Comp.Data.ReceiveFrequency == frequency)
            return;

        if (!TryGetNetwork(ent.Comp.DeviceNetId, out var deviceNet))
            return;

        var oldFrequency = ent.Comp.Data.ReceiveFrequency;
        deviceNet.Remove(ent!);
        ent.Comp.Data.ReceiveFrequency = frequency;
        deviceNet.Add(ent!);

        var ev = new DeviceReceiveFrequencyChangedEvent(oldFrequency, frequency);
        RaiseLocalEvent(ent, ref ev);

        DirtyField(ent, nameof(DeviceNetworkComponent.Data));
    }

    /// <summary>
    /// Sets the transmit frequency of an entity.
    /// </summary>
    /// <param name="ent">The target device.</param>
    /// <param name="frequency">The new frequency.</param>
    [PublicAPI]
    public void SetTransmitFrequency(Entity<DeviceNetworkComponent?> ent, uint? frequency)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        var oldFrequency = ent.Comp.Data.TransmitFrequency;
        ent.Comp.Data.TransmitFrequency = frequency;

        var ev = new DeviceReceiveFrequencyChangedEvent(oldFrequency, frequency);
        RaiseLocalEvent(ent, ref ev);

        DirtyField(ent, nameof(DeviceNetworkComponent.Data));
    }

    /// <summary>
    /// Sets the target device's ability to receive all network packets, regardless of the address.
    /// </summary>
    [PublicAPI]
    public void SetReceiveAll(Entity<DeviceNetworkComponent?> ent, bool receiveAll)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (ent.Comp.Data.ReceiveAll == receiveAll)
            return;

        if (!TryGetNetwork(ent.Comp.DeviceNetId, out var deviceNet))
            return;

        deviceNet.Remove(ent!);
        ent.Comp.Data.ReceiveAll = receiveAll;
        deviceNet.Add(ent!);

        var ev = new DeviceReceiveAllChangedEvent(receiveAll);
        RaiseLocalEvent(ent, ref ev);

        DirtyField(ent, nameof(DeviceNetworkComponent.Data));
    }

    /// <summary>
    /// Sets the address of the target device.
    /// </summary>
    [PublicAPI]
    public void SetAddress(Entity<DeviceNetworkComponent?> ent, string address)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (ent.Comp.Data.Address == address && ent.Comp.Data.CustomAddress)
            return;

        if (!TryGetNetwork(ent.Comp.DeviceNetId, out var deviceNet))
            return;

        var oldAddress = ent.Comp.Data.Address;
        deviceNet.Remove(ent!);
        ent.Comp.Data.CustomAddress = true;
        ent.Comp.Data.Address = address;
        deviceNet.Add(ent!);

        var ev = new DeviceAddressChangedEvent(oldAddress, address, ent.Comp.Data.CustomAddress);
        RaiseLocalEvent(ent, ref ev);

        DirtyField(ent, nameof(DeviceNetworkComponent.Data));
    }

    /// <summary>
    /// Randomizes the address of the target device.
    /// </summary>
    [PublicAPI]
    public void RandomizeAddress(Entity<DeviceNetworkComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!TryGetNetwork(ent.Comp.DeviceNetId, out var deviceNet))
            return;

        var oldAddress = ent.Comp.Data.Address;
        deviceNet.Remove(ent!);
        ent.Comp.Data.CustomAddress = false;
        ent.Comp.Data.Address = "";
        deviceNet.Add(ent!);

        var ev = new DeviceAddressChangedEvent(oldAddress, ent.Comp.Data.Address, ent.Comp.Data.CustomAddress);
        RaiseLocalEvent(ent, ref ev);

        DirtyField(ent, nameof(DeviceNetworkComponent.Data));
    }
}
