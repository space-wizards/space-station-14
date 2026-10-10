using System.Diagnostics.CodeAnalysis;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Components.Networks;
using Content.Shared.DeviceNetwork.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class DeviceNetworkSystem
{
    /// <summary>
    ///     Try to find a device on a network using its address.
    /// </summary>
    private static bool TryGetDevice(
        Entity<DeviceNetworkManagerComponent> manager,
        DeviceAddress address,
        [NotNullWhen(true)] out Device? device)
    {
        device = null;
        if (!manager.Comp.Devices.TryGetValue(address, out var foundDevice))
            return false;

        device = foundDevice;
        return true;
    }

    /// <summary>
    /// Tries to get an already existing device network, and creates a new network if it doesn't exist.
    /// </summary>
    private bool TryEnsureNetwork(
        Entity<DeviceNetworkComponent?> ent,
        ProtoId<DeviceNetworkPrototype> netId,
        [NotNullWhen(true)] out Entity<DeviceNetworkManagerComponent>? network)
    {
        network = null;
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp))
            return false;

        // Get an already connected network
        if (TryGetNetwork(ent, netId, out network))
            return true;

        // Connect to another already existing network that is available
        var query = EntityQueryEnumerator<DeviceNetworkManagerComponent>();
        while (query.MoveNext(out var uid, out var manager))
        {
            if (manager.DeviceNetId != netId)
                continue;

            var attemptEv = new DeviceAttemptConnectEvent(ent!);
            RaiseLocalEvent(uid, ref attemptEv);
            if (!attemptEv.Connected)
                continue;

            ent.Comp.ConnectedNets.Add(netId, uid);
            network = (uid, manager);
            return true;
        }

        // TODO removing this requires predicted entity spawning V2
        if (_net.IsClient)
            return false;

        // Create an entirely new network that has just this entity
        network = CreateNetwork(ent!, netId);
        return true;
    }

    /// <summary>
    /// Tries to get an already existing device network, and creates a new network if it doesn't exist.
    /// </summary>
    private Entity<DeviceNetworkManagerComponent>?[] EnsureNetworks(Entity<DeviceNetworkComponent?> ent)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp))
            return [];

        var arr = new Entity<DeviceNetworkManagerComponent>?[ent.Comp.DeviceNets.Length];
        for (var index = 0; index < ent.Comp.DeviceNets.Length; index++)
        {
            var netId = ent.Comp.DeviceNets[index];
            TryEnsureNetwork(ent, netId, out var network);
            arr[index] = network;
        }

        return arr;
    }

    private bool TryGetNetwork(
        Entity<DeviceNetworkComponent?> ent,
        ProtoId<DeviceNetworkPrototype> netId,
        [NotNullWhen(true)] out Entity<DeviceNetworkManagerComponent>? network)
    {
        network = null;
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (!ent.Comp.ConnectedNets.TryGetValue(netId, out var netUid) || !_deviceManagerQuery.TryComp(netUid, out var netComp))
            return false;

        network = (netUid, netComp);
        return true;
    }

    private Entity<DeviceNetworkManagerComponent>?[] GetNetworks(Entity<DeviceNetworkComponent?> ent)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp))
            return [];

        var arr = new Entity<DeviceNetworkManagerComponent>?[ent.Comp.DeviceNets.Length];
        for (var index = 0; index < ent.Comp.DeviceNets.Length; index++)
        {
            var netId = ent.Comp.DeviceNets[index];
            TryGetNetwork(ent, netId, out var network);
            arr[index] = network;
        }

        return arr;
    }

    private Entity<DeviceNetworkManagerComponent> CreateNetwork(Entity<DeviceNetworkComponent> ent, ProtoId<DeviceNetworkPrototype> proto)
    {
        var uid = Spawn(ProtoMan.Index(proto).ManagerId);

        var ev = new DeviceNetworkManagerInitializeEvent(ent);
        RaiseLocalEvent(uid, ref ev);

        var comp = Comp<DeviceNetworkManagerComponent>(uid);
        comp.DeviceNetId = proto;
        Dirty(uid, comp);

        // Purely for debug purposes
        _metaSystem.SetEntityName(uid, $"{Name(uid)} ({Loc.GetString(ProtoMan.Index(comp.DeviceNetId).Name)})");

        return (uid, comp);
    }

    /// <summary>
    /// Add a device to the network.
    /// </summary>
    private bool AddToNetwork(Entity<DeviceNetworkComponent?> ent, Entity<DeviceNetworkManagerComponent> network)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp))
            return false;

        var deviceComp = ent.Comp;
        var device = new Device((ent.Owner, ent.Comp));
        if (deviceComp.CustomAddress)
        {
            // Only add if the device's existing address is available.
            if (!network.Comp.Devices.TryAdd(deviceComp.Address, device))
                return false;
        }
        else
        {
            // Randomly generate a new address if the existing random one is invalid. Otherwise, keep the existing address
            if (deviceComp.Address == 0 || _occupiedAddresses.Contains(deviceComp.Address))
            {
                deviceComp.Address = GenerateValidAddressId();
                _occupiedAddresses.Add(deviceComp.Address.AddressId);
                device = new Device(ent!); // Reallocate because the data had changed
                DirtyField(ent, nameof(DeviceNetworkComponent.Address));
            }

            network.Comp.Devices[deviceComp.Address] = device;
        }

        if (deviceComp.ReceiveFrequency is not { } freq)
            return true;

        if (!network.Comp.ListeningDevices.TryGetValue(freq, out var devices))
            network.Comp.ListeningDevices[freq] = devices = new();

        devices.Add(device);

        if (!deviceComp.ReceiveAll)
            return true;

        if (!network.Comp.ReceiveAllDevices.TryGetValue(freq, out var receiveAlldevices))
            network.Comp.ReceiveAllDevices[freq] = receiveAlldevices = new();

        receiveAlldevices.Add(device);
        return true;
    }

    /// <summary>
    /// Removes a device from the network.
    /// </summary>
    private bool RemoveFromNetwork(Entity<DeviceNetworkComponent?> ent, Entity<DeviceNetworkManagerComponent> network)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp))
            return false;

        var deviceComp = ent.Comp;
        var device = new Device(ent!);
        if (!network.Comp.Devices.Remove(deviceComp.Address))
            return false;

        if (deviceComp.ReceiveFrequency is not { } freq)
            return true;

        if (network.Comp.ListeningDevices.TryGetValue(freq, out var listening))
        {
            listening.Remove(device);
            if (listening.Count == 0)
                network.Comp.ListeningDevices.Remove(freq);
        }

        if (deviceComp.ReceiveAll && network.Comp.ReceiveAllDevices.TryGetValue(freq, out var receiveAll))
        {
            receiveAll.Remove(device);
            if (receiveAll.Count == 0)
                network.Comp.ListeningDevices.Remove(freq);
        }

        return true;
    }

    /// <summary>
    /// Generates a valid address by randomly generating one and checking if it already exists on the network.
    /// </summary>
    private DeviceAddress GenerateValidAddressId()
    {
        // I hate this being O(n), but it's not like there will be millions of devices, right?
        int addressId;
        do
        {
            // There is a 1 in 2 billion chance to roll a 0.
            // Would be funny for this to stay as a super-gamble test fail, but I am evil no fun on my evil Space Station
            addressId = _random.Next();
        } while (_occupiedAddresses.Contains(addressId) || addressId == DeviceAddress.Invalid.AddressId);

        return new DeviceAddress(addressId);
    }
}
