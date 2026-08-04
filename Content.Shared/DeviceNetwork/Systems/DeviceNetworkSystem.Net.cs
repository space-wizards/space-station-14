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
    private bool TryGetDevice(
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

        if (TryGetNetwork(ent, netId, out network))
            return true;

        // TODO removing this requires predicted entity spawning V2
        if (_net.IsClient)
            return false;

        network = CreateNetwork(ent!, netId);
        return true;
    }

    /// <summary>
    /// Tries to get an already existing device network, and creates a new network if it doesn't exist.
    /// </summary>
    private bool TryEnsureNetwork(
        Entity<DeviceNetworkComponent?> ent,
        [NotNullWhen(true)] out Entity<DeviceNetworkManagerComponent>? network)
    {
        network = null;
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        return TryEnsureNetwork(ent, ent.Comp.DeviceNetId, out network);
    }

    private bool TryGetNetwork(
        Entity<DeviceNetworkComponent?> ent,
        ProtoId<DeviceNetworkPrototype> netId,
        [NotNullWhen(true)] out Entity<DeviceNetworkManagerComponent>? network)
    {
        network = null;
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        var query = EntityQueryEnumerator<DeviceNetworkManagerComponent>();
        while (query.MoveNext(out var uid, out var manager))
        {
            if (manager.DeviceNetId != netId)
                continue;

            var attemptEv = new DeviceAttemptConnectEvent(ent!);
            RaiseLocalEvent(uid, ref attemptEv);
            if (!attemptEv.Connected)
                continue;

            network = (uid, manager);
            return true;
        }

        return false;
    }

    private bool TryGetNetwork(
        Entity<DeviceNetworkComponent?> ent,
        [NotNullWhen(true)] out Entity<DeviceNetworkManagerComponent>? network)
    {
        network = null;
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        return TryGetNetwork(ent, ent.Comp.DeviceNetId, out network);
    }

    private Entity<DeviceNetworkManagerComponent> CreateNetwork(Entity<DeviceNetworkComponent> ent, ProtoId<DeviceNetworkPrototype> proto)
    {
        var uid = Spawn(ProtoMan.Index(proto).ManagerId);

        var ev = new DeviceNetworkManagerInitializeEvent(ent);
        RaiseLocalEvent(uid, ref ev);

        var comp = Comp<DeviceNetworkManagerComponent>(uid);
        comp.DeviceNetId = proto;

        // Purely for debug purposes
        _meta.SetEntityName(uid, $"{Name(uid)} ({Loc.GetString(ProtoMan.Index(comp.DeviceNetId).Name)})");

        return (uid, comp);
    }

    /// <summary>
    /// Add a device to the network.
    /// </summary>
    private bool AddToNetwork(Entity<DeviceNetworkComponent?> ent, Entity<DeviceNetworkManagerComponent> manager)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp))
            return false;

        var network = manager.Comp;
        var deviceComp = ent.Comp;
        var device = new Device(ent.Owner, ent.Comp.Data);
        if (deviceComp.Data.CustomAddress)
        {
            // Only add if the device's existing address is available.
            if (!network.Devices.TryAdd(deviceComp.Data.AddressId, device))
                return false;
        }
        else
        {
            // Randomly generate a new address if the existing random one is invalid. Otherwise, keep the existing address
            if (deviceComp.Data.AddressId == 0 || network.Devices.ContainsKey(deviceComp.Data.AddressId))
            {
                deviceComp.Data.AddressId = GenerateValidAddressId(network);
                device = new Device(ent.Owner, ent.Comp.Data); // Reallocate because the data had changed
            }

            network.Devices[deviceComp.Data.AddressId] = device;
        }

        if (deviceComp.Data.ReceiveFrequency is not { } freq)
            return true;

        if (!network.ListeningDevices.TryGetValue(freq, out var devices))
            network.ListeningDevices[freq] = devices = [];

        devices.Add(device);

        if (!deviceComp.Data.ReceiveAll)
            return true;

        if (!network.ReceiveAllDevices.TryGetValue(freq, out var receiveAlldevices))
            network.ReceiveAllDevices[freq] = receiveAlldevices = [];

        receiveAlldevices.Add(device);
        return true;
    }

    /// <summary>
    /// Removes a device from the network.
    /// </summary>
    private bool RemoveFromNetwork(Entity<DeviceNetworkComponent?> ent, Entity<DeviceNetworkManagerComponent> manager)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp))
            return false;

        var network = manager.Comp;
        var deviceComp = ent.Comp;
        var device = new Device(ent.Owner, ent.Comp.Data);
        if (!network.Devices.Remove(deviceComp.Data.AddressId))
            return false;

        if (device.DeviceData.ReceiveFrequency is not { } freq)
            return true;

        if (network.ListeningDevices.TryGetValue(freq, out var listening))
        {
            listening.Remove(device);
            if (listening.Count == 0)
                network.ListeningDevices.Remove(freq);
        }

        if (device.DeviceData.ReceiveAll && network.ReceiveAllDevices.TryGetValue(freq, out var receiveAll))
        {
            receiveAll.Remove(device);
            if (receiveAll.Count == 0)
                network.ListeningDevices.Remove(freq);
        }

        return true;
    }

    /// <summary>
    /// Generates a valid address by randomly generating one and checking if it already exists on the network.
    /// </summary>
    private DeviceAddress GenerateValidAddressId(DeviceNetworkManagerComponent network)
    {
        DeviceAddress addressId;
        do
        {
            // There is a 1 in 2 billion chance to roll a 0.
            // Would be funny for this to stay as a super-gamble test fail, but I am evil no fun on my evil Space Station
            addressId = _random.Next();
        } while (network.Devices.ContainsKey(addressId) || addressId == DeviceAddress.Invalid);

        return addressId;
    }
}
