using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Components.Networks;
using Content.Shared.DeviceNetwork.Events;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

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
    public bool SendPacket<T>(
        Entity<DeviceNetworkComponent?> ent,
        DeviceAddress? address,
        ref T data,
        DeviceFrequency? frequency = null,
        ProtoId<DeviceNetworkPrototype>? network = null)
        where T : INetworkPayload
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        var device = ent.Comp;
        if (device.Data.AddressId == 0)
            return false;

        frequency ??= device.TransmitFrequency;

        if (frequency == null)
            return false;

        network ??= ent.Comp.DeviceNetId;

        var packet = new DeviceNetworkPacketEvent<T>(network.Value, address, frequency.Value, device.Data.AddressId, ent!, data);
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
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (!TryEnsureNetwork(ent, out var deviceNet))
            return false;

        var success = AddToNetwork(ent, deviceNet.Value);
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
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (!TryGetNetwork(ent, out var deviceNet))
            return false;

        if (preventAutoConnect)
            ent.Comp.AutoConnect = false;

        return RemoveFromNetwork(ent, deviceNet.Value);
    }

    /// <summary>
    /// Reconnects the device, possibly to a new device network.
    /// This should be called when the conditions under which the device networks are formed may change for an entity.
    /// </summary>
    [PublicAPI]
    public void ReconnectDevice(Entity<DeviceNetworkComponent?> ent)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (TryGetNetwork(ent, out var oldDeviceNet))
            RemoveFromNetwork(ent, oldDeviceNet.Value);

        if (!TryEnsureNetwork(ent, out var deviceNet))
            return; // Client-side

        AddToNetwork(ent, deviceNet.Value);

        DirtyField(ent, nameof(DeviceNetworkComponent.Data));
    }

    /// <summary>
    /// Checks if a device is already connected to its network.
    /// </summary>
    /// <returns>True if the device was found in the network with its corresponding network id.</returns>
    [PublicAPI]
    public bool IsDeviceConnected(Entity<DeviceNetworkComponent?> ent)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (!TryGetNetwork(ent, out var deviceNet))
            return false;

        var device = new Device(ent.Owner, ent.Comp.Data);
        return deviceNet.Value.Comp.Devices.ContainsValue(device);
    }

    /// <summary>
    /// Checks if an address exists in the network with the given netId.
    /// </summary>
    [PublicAPI]
    public bool IsAddressPresent(Entity<DeviceNetworkComponent?> ent, DeviceAddress? address)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (!TryGetNetwork(ent, out var deviceNet))
            return false;

        return IsAddressPresent(deviceNet.Value, address);
    }

    /// <summary>
    /// Checks if an address exists in the network with the given netId.
    /// </summary>
    [PublicAPI]
    public bool IsAddressPresent(Entity<DeviceNetworkManagerComponent> manager, DeviceAddress? address)
    {
        if (address == null)
            return false;

        return manager.Comp.Devices.ContainsKey(address.Value);
    }

    /// <summary>
    /// Sets the receive frequency of an entity.
    /// </summary>
    /// <param name="ent">The target device.</param>
    /// <param name="frequency">The new frequency.</param>
    [PublicAPI]
    public void SetReceiveFrequency(Entity<DeviceNetworkComponent?> ent, DeviceFrequency? frequency)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (ent.Comp.Data.ReceiveFrequency == frequency)
            return;

        if (!TryEnsureNetwork(ent, out var deviceNet))
            return;

        var oldFrequency = ent.Comp.Data.ReceiveFrequency;
        RemoveFromNetwork(ent, deviceNet.Value);
        ent.Comp.Data.ReceiveFrequency = frequency;
        AddToNetwork(ent, deviceNet.Value);

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
    public void SetTransmitFrequency(Entity<DeviceNetworkComponent?> ent, DeviceFrequency? frequency)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return;

        var oldFrequency = ent.Comp.TransmitFrequency;
        ent.Comp.TransmitFrequency = frequency;

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
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (ent.Comp.Data.ReceiveAll == receiveAll)
            return;

        if (!TryEnsureNetwork(ent, out var deviceNet))
            return;

        RemoveFromNetwork(ent, deviceNet.Value);
        ent.Comp.Data.ReceiveAll = receiveAll;
        AddToNetwork(ent, deviceNet.Value);

        var ev = new DeviceReceiveAllChangedEvent(receiveAll);
        RaiseLocalEvent(ent, ref ev);

        DirtyField(ent, nameof(DeviceNetworkComponent.Data));
    }

    /// <summary>
    /// Sets the address of the target device.
    /// </summary>
    [PublicAPI]
    public void SetAddress(Entity<DeviceNetworkComponent?> ent, DeviceAddress address)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (ent.Comp.Data.AddressId == address && ent.Comp.CustomAddress)
            return;

        if (!TryEnsureNetwork(ent, out var deviceNet))
            return;

        var oldAddress = ent.Comp.Data.AddressId;
        var oldPrefix = ent.Comp.Prefix;

        RemoveFromNetwork(ent, deviceNet.Value);
        ent.Comp.CustomAddress = true;
        ent.Comp.Data.AddressId = address;
        if (prefix != null)
            ent.Comp.Prefix = prefix;
        AddToNetwork(ent, deviceNet.Value);

        var ev = new DeviceAddressChangedEvent(oldAddress, address, oldPrefix, ent.Comp.Prefix, ent.Comp.CustomAddress);
        RaiseLocalEvent(ent, ref ev);

        DirtyFields(ent, null, nameof(DeviceNetworkComponent.Data), nameof(DeviceNetworkComponent.CustomAddress));
    }

    /// <summary>
    /// Sets the address prefix of the target device.
    /// </summary>
    [PublicAPI]
    public void SetAddressPrefix(Entity<DeviceNetworkComponent?> ent, LocId? prefix)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return;

        ent.Comp.Prefix = prefix;
        DirtyField(ent, nameof(DeviceNetworkComponent.Prefix));
    }

    /// <summary>
    /// Randomizes the address of the target device.
    /// </summary>
    [PublicAPI]
    public void RandomizeAddress(Entity<DeviceNetworkComponent?> ent)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (!TryEnsureNetwork(ent, out var deviceNet))
            return;

        var oldAddress = ent.Comp.Data.AddressId;
        RemoveFromNetwork(ent, deviceNet.Value);
        ent.Comp.CustomAddress = false;
        ent.Comp.Data.AddressId = 0;
        AddToNetwork(ent, deviceNet.Value);

        var ev = new DeviceAddressChangedEvent(oldAddress, ent.Comp.Data.AddressId, ent.Comp.Prefix, ent.Comp.Prefix, ent.Comp.CustomAddress);
        RaiseLocalEvent(ent, ref ev);

        DirtyFields(ent, null, nameof(DeviceNetworkComponent.Data), nameof(DeviceNetworkComponent.CustomAddress));
    }

    /// <summary>
    /// Gets the visible address as a string for the players to see.
    /// </summary>
    /// <param name="ent">The device to get the address of.</param>
    /// <returns>The player-facing representation of an address.</returns>
    [PublicAPI]
    public string GetAddress(Entity<DeviceNetworkComponent?> ent)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return string.Empty;

        return DeviceLocalizationHelpers.GetAddressFromId(ent.Comp.Data.AddressId, ent.Comp.Prefix);
    }
}
