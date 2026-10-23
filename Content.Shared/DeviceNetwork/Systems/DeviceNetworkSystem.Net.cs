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

            var attemptEv = new DeviceAttemptConnectEvent(ent!, manager);
            RaiseLocalEvent(uid, ref attemptEv);
            if (!attemptEv.Connected)
                continue;

            network = (uid, manager);
            return true;
        }

        // Create an entirely new network that has just this entity
        network = CreateNetwork(ent!, netId);
        return true;
    }

    /// <summary>
    /// Tries to get an already existing device network, and creates a new network if it doesn't exist.
    /// </summary>
    private void EnsureNetworks(Entity<DeviceNetworkComponent?> ent, List<Entity<DeviceNetworkManagerComponent>> cache)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        foreach (var netId in ent.Comp.DeviceNets)
        {
            if (TryEnsureNetwork(ent, netId, out var network))
                cache.Add(network.Value);
        }
    }

    private bool TryGetNetwork(
        Entity<DeviceNetworkComponent?> ent,
        ProtoId<DeviceNetworkPrototype> netId,
        [NotNullWhen(true)] out Entity<DeviceNetworkManagerComponent>? network)
    {
        network = null;
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (!ent.Comp.ConnectedNets.TryGetValue(netId, out var netUid)
            || !_deviceManagerQuery.TryComp(netUid, out var netComp))
            return false;

        network = (netUid, netComp);
        return true;
    }

    private void GetNetworks(Entity<DeviceNetworkComponent?> ent, List<Entity<DeviceNetworkManagerComponent>> cache)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp))
            return;

        foreach (var netId in ent.Comp.DeviceNets)
        {
            if (TryGetNetwork(ent, netId, out var network))
                cache.Add(network.Value);
        }
    }

    private Entity<DeviceNetworkManagerComponent> CreateNetwork(Entity<DeviceNetworkComponent> ent, ProtoId<DeviceNetworkPrototype> proto)
    {
        var uid = PredictedSpawn(ProtoMan.Index(proto).ManagerId);

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
    private bool AddToNetwork(Entity<DeviceNetworkComponent> ent, Entity<DeviceNetworkManagerComponent> network)
    {
        // Checking for a key because on client the EntityUid may be wrong for the first tick if it was spawned predictively
        if (_net.IsClient && ent.Comp.ConnectedNets.ContainsKey(network.Comp.DeviceNetId))
            return true;

        if (ent.Comp.ConnectedNets.TryGetValue(network.Comp.DeviceNetId, out var netUid)
            && netUid != network.Owner)
        {
            Log.Error($"Device {ToPrettyString(ent)} tried to connect to the network" +
                      $"{ToPrettyString(network.Owner)} while it was already connected to {ToPrettyString(netUid)}!");
        }

        ent.Comp.ConnectedNets.TryAdd(network.Comp.DeviceNetId, network.Owner);

        var deviceComp = ent.Comp;
        var device = new Device((ent.Owner, ent.Comp));

        if (!network.Comp.Devices.TryAdd(deviceComp.Address, device))
            return false;

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
        } while (_occupiedAddresses.ContainsKey(addressId) || addressId == DeviceAddress.Invalid.AddressId);

        return new DeviceAddress(addressId);
    }
}
