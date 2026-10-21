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

        if (ent.Comp.JammedTimeout != null
            && _timing.CurTime < ent.Comp.JammedTimeout.Value)
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

        if (!ent.Comp.Enabled)
            return false;

        EnsureValidAddress(ent);

        _managerCache.Clear();
        EnsureNetworks(ent, _managerCache);

        var success = false;
        foreach (var net in _managerCache)
        {
            if (AddToNetwork(ent!, net))
                success = true;
        }

        return success;
    }

    /// <summary>
    /// Ensures that the specified entity has a valid <see cref="DeviceNetworkComponent.Address"/>
    /// that isn't a duplicate or <see cref="DeviceAddress.Invalid"/>.
    /// </summary>
    [PublicAPI]
    public void EnsureValidAddress(Entity<DeviceNetworkComponent?> ent)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        if (ent.Comp.Address == DeviceAddress.Invalid)
        {
            ent.Comp.Address = GenerateValidAddressId();
            DirtyField(ent.AsNullable(), nameof(DeviceNetworkComponent.Address));
        }

        if (_occupiedAddresses.TryGetValue(ent.Comp.Address.AddressId, out var existing)
            && existing != ent.Owner)
        {
            if (TerminatingOrDeleted(existing))
                _occupiedAddresses.Remove(ent.Comp.Address.AddressId); // Entity was deleted and therefore the address is free
            else
            {
                // This should only happen with corrupted game saves, otherwise it's a bug that needs to be reported.
                Log.Warning(
                    $"Device {ToPrettyString(ent.Owner)} has a duplicate address {ent.Comp.Address.ToString()} with entity {ToPrettyString(existing)}." +
                    $"Replacing it with another address. This may lead to unexpected errors.");
                ent.Comp.Address = GenerateValidAddressId();
                DirtyField(ent.AsNullable(), nameof(DeviceNetworkComponent.Address));
            }
        }

        _occupiedAddresses.TryAdd(ent.Comp.Address.AddressId, ent.Owner);
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

        if (!ent.Comp.Enabled)
            return true; // Already disconnected, so it's a success

        if (MetaData(ent.Owner).EntityLifeStage >= EntityLifeStage.MapInitialized && preventAutoConnect)
        {
            ent.Comp.AutoConnect = false;
            DirtyField(ent, nameof(DeviceNetworkComponent.AutoConnect));
        }

        _managerCache.Clear();
        GetNetworks(ent, _managerCache);

        var success = false;
        foreach (var net in _managerCache)
        {
            if (RemoveFromNetwork(ent, net))
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

        _managerCache.Clear();
        GetNetworks(ent, _managerCache);
        foreach (var net in _managerCache)
        {
            var device = new Device(ent!);
            if (net.Comp.Devices.ContainsValue(device))
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
        return address != null && _occupiedAddresses.ContainsKey(address.Value.AddressId);
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

        _managerCache.Clear();
        GetNetworks(ent, _managerCache);
        foreach (var net in _managerCache)
        {
            if (net.Comp.Devices.ContainsKey(address.Value))
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

        if (ent.Comp.Address == address)
            return;

        var oldAddress = ent.Comp.Address;

        ent.Comp.Address = address;

        ReconnectDevice(ent);

        var ev = new DeviceAddressChangedEvent(oldAddress, address);
        RaiseLocalEvent(ent, ref ev);

        DirtyField(ent, nameof(DeviceNetworkComponent.Address));
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
        ent.Comp.Address = DeviceAddress.Invalid;

        ReconnectDevice(ent);

        var ev = new DeviceAddressChangedEvent(oldAddress, ent.Comp.Address);
        RaiseLocalEvent(ent, ref ev);

        DirtyField(ent, nameof(DeviceNetworkComponent.Address));
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

    /// <summary>
    /// Sets a device to be enabled or disabled.
    /// While disabled, the device can't connect to any device networks.
    /// </summary>
    /// <param name="ent">The target device.</param>
    /// <param name="enabled">
    /// If true, tries to connect the device to networks.
    /// If false, disconnects the device from networks and doesn't allow to connect until set to true.
    /// </param>
    [PublicAPI]
    public void SetEnabled(Entity<DeviceNetworkComponent?> ent, bool enabled)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return;

        if (enabled)
            ConnectDevice(ent);
        else
            DisconnectDevice(ent);

        ent.Comp.Enabled = enabled;
        DirtyField(ent, nameof(DeviceNetworkComponent.Enabled));
    }

    /// <summary>
    /// Checks whether a <see cref="ILimitedDeviceComponent"/> should not be applied for the current network.
    /// </summary>
    /// <param name="ent">The target entity with a <see cref="DeviceNetworkComponent"/>.</param>
    /// <param name="netId"><see cref="DeviceNetworkPrototype"/> ID to check.</param>
    /// <typeparam name="T">Component that implements <see cref="ILimitedDeviceComponent"/>.</typeparam>
    /// <returns>True if this network shouldn't be checked, false if the target network must be checked for blocking.</returns>
    [PublicAPI]
    public bool CheckNetworkIgnore<T>(Entity<T, DeviceNetworkComponent?> ent, ProtoId<DeviceNetworkPrototype> netId)
        where T : IComponent, ILimitedDeviceComponent
    {
        return !_deviceQuery.Resolve(ent.Owner, ref ent.Comp2, false) || ent.Comp1.IgnoreNets.Contains(netId);
    }

    [PublicAPI]
    public bool CheckNetworkIgnore<T>(DeviceAttemptConnectEvent args, EntityQuery<T> query) where T : IComponent, ILimitedDeviceComponent
    {
        return query.TryComp(args.Entity, out var comp)
               && CheckNetworkIgnore<T>((args.Entity.Owner, comp, args.Entity.Comp), args.ManagerComp.DeviceNetId);
    }
}
