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
    /// <param name="address">
    /// The address of the entity that the packet gets sent to.
    /// If null, the message is broadcast to all devices on that frequency (except the sender)
    /// </param>
    /// <param name="data">The data to be sent.</param>
    /// <param name="frequency">The frequency to send on.</param>
    /// <param name="network">The network to send on.</param>
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
        if (device.Address == DeviceAddress.Invalid)
            return false;

        frequency ??= device.TransmitFrequency;

        if (frequency == null)
            return false;

        if (network != null)
        {
            // Unsupported network type
            if (!ent.Comp.DeviceNets.Contains(network.Value))
                return false;

            var packet = new DeviceNetworkPacketEvent<T>(network.Value,
                address,
                frequency.Value,
                device.Address,
                ent!,
                data);
            SendPacket(ref packet);
        }
        else
        {
            foreach (var net in ent.Comp.DeviceNets)
            {
                var packet = new DeviceNetworkPacketEvent<T>(net,
                    address,
                    frequency.Value,
                    device.Address,
                    ent!,
                    data);
                SendPacket(ref packet);
            }
        }


        return true;
    }

    /// <summary>
    /// Sends the given payload as a device network packet to the entity with the given address and frequency.
    /// Addresses are given to the DeviceNetworkComponent of an entity when connecting.
    /// </summary>
    /// <param name="ent">The sending entity</param>
    /// <param name="address">The address of the entity that the packet gets sent to. If null, the message is broadcast to all devices on that frequency (except the sender)</param>
    /// <param name="frequency">The frequency to send on</param>
    /// <param name="data">The data to be sent</param>
    /// <param name="networks">Device network override</param>
    /// <returns>Returns true when the packet was successfully enqueued.</returns>
    [PublicAPI]
    public bool SendPacketToNetworks<T>(
        Entity<DeviceNetworkComponent?> ent,
        DeviceAddress? address,
        ref T data,
        DeviceFrequency? frequency = null,
        params ProtoId<DeviceNetworkPrototype>[] networks)
        where T : INetworkPayload
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        // Device is 100% disconnected
        if (ent.Comp.Address == DeviceAddress.Invalid)
            return false;

        frequency ??= ent.Comp.TransmitFrequency;

        // Unspecified frequency
        if (frequency == null)
            return false;

        foreach (var net in networks)
        {
            var packet = new DeviceNetworkPacketEvent<T>(net, address, frequency.Value, ent.Comp.Address, ent!, data);
            SendPacket(ref packet);
        }

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

        var deviceNets = EnsureNetworks(ent);

        var success = false;
        foreach (var net in deviceNets)
        {
            if (net == null)
                continue;

            if (AddToNetwork(ent, net.Value))
                success = true;
        }

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

        if (preventAutoConnect)
        {
            ent.Comp.AutoConnect = false;
            DirtyField(ent, nameof(DeviceNetworkComponent.AutoConnect));
        }

        var deviceNets = GetNetworks(ent);

        var success = false;
        foreach (var net in deviceNets)
        {
            if (net == null)
                continue;

            if (RemoveFromNetwork(ent, net.Value))
                success = true;
        }

        return success;
    }

    /// <summary>
    /// Reconnects the device, possibly to a new device network.
    /// This should be called when the conditions under which the device networks are formed may change for an entity.
    /// </summary>
    [PublicAPI]
    public void ReconnectDevice(Entity<DeviceNetworkComponent?> ent)
    {
        DisconnectDevice(ent);
        ConnectDevice(ent);
    }

    /// <summary>
    /// Checks if a device is already connected to any network.
    /// </summary>
    /// <returns>True if the device was found in any network with its corresponding network id.</returns>
    [PublicAPI]
    public bool IsDeviceConnected(Entity<DeviceNetworkComponent?> ent)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        foreach (var net in GetNetworks(ent))
        {
            if (net == null)
                continue;

            var device = new Device(ent!);
            if (net.Value.Comp.Devices.ContainsValue(device))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if an address exists in any network.
    /// </summary>
    [PublicAPI]
    public bool IsAddressPresent(DeviceAddress? address)
    {
        return address != null && _occupiedAddresses.Contains(address.Value.AddressId);
    }

    /// <summary>
    /// Checks if an address exists in the given network.
    /// </summary>
    [PublicAPI]
    public bool IsAddressPresent(Entity<DeviceNetworkManagerComponent> manager, DeviceAddress? address)
    {
        return address != null && manager.Comp.Devices.ContainsKey(address.Value);
    }

    /// <summary>
    /// Checks if an address exists in any given network of the target entity.
    /// </summary>
    [PublicAPI]
    public bool IsAddressPresent(Entity<DeviceNetworkComponent?> ent, DeviceAddress? address)
    {
        if (address == null)
            return false;

        foreach (var net in GetNetworks(ent))
        {
            if (net != null && net.Value.Comp.Devices.ContainsKey(address.Value))
                return true;
        }

        return false;
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

        if (ent.Comp.ReceiveFrequency == frequency)
            return;

        var oldFrequency = ent.Comp.ReceiveFrequency;
        ent.Comp.ReceiveFrequency = frequency;
        ReconnectDevice(ent);

        var ev = new DeviceReceiveFrequencyChangedEvent(oldFrequency, frequency);
        RaiseLocalEvent(ent, ref ev);

        DirtyField(ent, nameof(DeviceNetworkComponent.ReceiveFrequency));
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

        if (ent.Comp.TransmitFrequency == frequency)
            return;

        var oldFrequency = ent.Comp.TransmitFrequency;
        if (oldFrequency == frequency)
            return;

        ent.Comp.TransmitFrequency = frequency;

        var ev = new DeviceTransmitFrequencyChangedEvent(oldFrequency, frequency);
        RaiseLocalEvent(ent, ref ev);

        DirtyField(ent, nameof(DeviceNetworkComponent.TransmitFrequency));
    }

    /// <summary>
    /// Sets the target device's ability to receive all network packets, regardless of the address.
    /// </summary>
    [PublicAPI]
    public void SetReceiveAll(Entity<DeviceNetworkComponent?> ent, bool receiveAll)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (ent.Comp.ReceiveAll == receiveAll)
            return;

        ent.Comp.ReceiveAll = receiveAll;
        ReconnectDevice(ent);

        var ev = new DeviceReceiveAllChangedEvent(receiveAll);
        RaiseLocalEvent(ent, ref ev);

        DirtyField(ent, nameof(DeviceNetworkComponent.ReceiveAll));
    }

    /// <summary>
    /// Sets the address of the target device.
    /// </summary>
    [PublicAPI]
    public void SetAddress(Entity<DeviceNetworkComponent?> ent, DeviceAddress address)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (ent.Comp.Address == address && ent.Comp.CustomAddress)
            return;

        var oldAddress = ent.Comp.Address;

        ent.Comp.CustomAddress = true;
        ent.Comp.Address = address;

        ReconnectDevice(ent);

        var ev = new DeviceAddressChangedEvent(oldAddress, address, ent.Comp.CustomAddress);
        RaiseLocalEvent(ent, ref ev);

        DirtyFields(ent, null, nameof(DeviceNetworkComponent.Address), nameof(DeviceNetworkComponent.CustomAddress));
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

        var oldAddress = ent.Comp.Address;
        ent.Comp.CustomAddress = false;
        ent.Comp.Address = DeviceAddress.Invalid;

        ReconnectDevice(ent);

        var ev = new DeviceAddressChangedEvent(oldAddress, ent.Comp.Address, ent.Comp.CustomAddress);
        RaiseLocalEvent(ent, ref ev);

        DirtyFields(ent, null, nameof(DeviceNetworkComponent.Address), nameof(DeviceNetworkComponent.CustomAddress));
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

        return DeviceLocalizationHelpers.GetAddressFromId(ent.Comp.Address, ent.Comp.Prefix);
    }
}
