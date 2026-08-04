using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using JetBrains.Annotations;

namespace Content.Server.DeviceNetwork.Systems;

public sealed partial class DeviceNetworkSystem
{
    [PublicAPI]
    public override bool SendPacket<T>(
        Entity<DeviceNetworkComponent?> ent,
        string? address,
        ref T data,
        uint? frequency = null,
        int? network = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        var device = ent.Comp;
        if (device.Data.Address == string.Empty)
            return false;

        frequency ??= device.TransmitFrequency;

        if (frequency == null)
            return false;

        network ??= device.DeviceNetId;

        var packet = new DeviceNetworkPacketEvent<T>(network.Value, address, frequency.Value, device.Data.Address, ent!, data);
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
        Dirty(ent);
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

        var result = deviceNet.Remove(ent!);
        return result;
    }

    /// <summary>
    /// Checks if a device is already connected to its network.
    /// </summary>
    /// <returns>True if the device was found in the network with its corresponding network id.</returns>
    [PublicAPI]
    public bool IsDeviceConnected(Entity<DeviceNetworkComponent?> ent)
    {
        var (uid, deviceComp) = ent;
        if (!Resolve(uid, ref deviceComp, false))
            return false;

        if (!_networks.TryGetValue(deviceComp.DeviceNetId, out var deviceNet))
            return false;

        var device = new Device(uid, deviceComp.Data);
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

        Dirty(ent);
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

        var oldFrequency = ent.Comp.TransmitFrequency;
        ent.Comp.TransmitFrequency = frequency;

        var ev = new DeviceReceiveFrequencyChangedEvent(oldFrequency, frequency);
        RaiseLocalEvent(ent, ref ev);
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

        Dirty(ent);
    }

    /// <summary>
    /// Sets the address of the target device.
    /// </summary>
    [PublicAPI]
    public void SetAddress(Entity<DeviceNetworkComponent?> ent, string address)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (ent.Comp.Data.Address == address && ent.Comp.CustomAddress)
            return;

        if (!TryGetNetwork(ent.Comp.DeviceNetId, out var deviceNet))
            return;

        var oldAddress = ent.Comp.Data.Address;
        deviceNet.Remove(ent!);
        ent.Comp.CustomAddress = true;
        ent.Comp.Data.Address = address;
        deviceNet.Add(ent!);

        var ev = new DeviceAddressChangedEvent(oldAddress, address, ent.Comp.CustomAddress);
        RaiseLocalEvent(ent, ref ev);

        Dirty(ent);
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
        ent.Comp.CustomAddress = false;
        ent.Comp.Data.Address = "";
        deviceNet.Add(ent!);

        var ev = new DeviceAddressChangedEvent(oldAddress, ent.Comp.Data.Address, ent.Comp.CustomAddress);
        RaiseLocalEvent(ent, ref ev);

        Dirty(ent);
    }
}
