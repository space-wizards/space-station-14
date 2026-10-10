using Content.Shared.DeviceNetwork.Components;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class DeviceNetworkSystem
{
    /// <summary>
    /// Add a device to the network.
    /// </summary>
    private bool AddToNetwork(Entity<DeviceNetworkComponent?> ent, DeviceNet network)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp))
            return false;

        var deviceComp = ent.Comp;
        var device = new Device((ent.Owner, ent.Comp));
        if (deviceComp.CustomAddress)
        {
            // Only add if the device's existing address is available.
            if (!network.Devices.TryAdd(deviceComp.Address, device))
                return false;
        }
        else
        {
            // Randomly generate a new address if the existing random one is invalid. Otherwise, keep the existing address
            if (deviceComp.Address == 0 || network.Devices.ContainsKey(deviceComp.Address))
            {
                deviceComp.Address = GenerateValidAddressId(network);
                device = new Device((ent.Owner, ent.Comp)); // Reallocate because the data had changed
            }

            network.Devices[deviceComp.Address] = device;
        }

        if (deviceComp.ReceiveFrequency is not { } freq)
            return true;

        if (!network.ListeningDevices.TryGetValue(freq, out var devices))
            network.ListeningDevices[freq] = devices = new();

        devices.Add(device);

        if (!deviceComp.ReceiveAll)
            return true;

        if (!network.ReceiveAllDevices.TryGetValue(freq, out var receiveAlldevices))
            network.ReceiveAllDevices[freq] = receiveAlldevices = new();

        receiveAlldevices.Add(device);
        return true;
    }

    /// <summary>
    /// Removes a device from the network.
    /// </summary>
    private bool RemoveFromNetwork(Entity<DeviceNetworkComponent?> ent, DeviceNet network)
    {
        if (!_deviceQuery.Resolve(ent.Owner, ref ent.Comp))
            return false;

        var deviceComp = ent.Comp;
        var device = new Device((ent.Owner, ent.Comp));
        if (!network.Devices.Remove(deviceComp.Address))
            return false;

        if (deviceComp.ReceiveFrequency is not { } freq)
            return true;

        if (network.ListeningDevices.TryGetValue(freq, out var listening))
        {
            listening.Remove(device);
            if (listening.Count == 0)
                network.ListeningDevices.Remove(freq);
        }

        if (deviceComp.ReceiveAll && network.ReceiveAllDevices.TryGetValue(freq, out var receiveAll))
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
    private DeviceAddress GenerateValidAddressId(DeviceNet network)
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
