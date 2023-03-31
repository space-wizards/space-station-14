using Content.Server.DeviceNetwork.Components;
using Robust.Shared.Random;
using static Content.Server.DeviceNetwork.Components.DeviceNetworkComponent;

namespace Content.Server.DeviceNetwork;

/// <summary>
///     Data class for storing and retrieving information about devices connected to a device network.
/// </summary>
/// <remarks>
///     This basically just makes <see cref="DeviceNetworkComponent"/> accessible via their addresses and frequencies on
///     some network.
/// </remarks>
public sealed class DeviceNet
{
    /// <summary>
    ///     Devices, mapped by their "Address", which is just an int that gets converted to Hex for displaying to users.
    ///     This dictionary contains all devices connected to this network, though they may not be listening to any
    ///     specific frequency.
    /// </summary>
    public readonly Dictionary<string, DeviceNetworkComponent> Devices = new();

    /// <summary>
    ///     Devices listening on a given frequency.
    /// </summary>
    public readonly Dictionary<uint, HashSet<DeviceNetworkComponent>> ListeningDevices = new();

    /// <summary>
    ///     Devices listening to all packets on a given frequency, regardless of the intended recipient.
    /// </summary>
    public readonly Dictionary<uint, HashSet<DeviceNetworkComponent>> ReceiveAllDevices = new();

    private readonly IRobustRandom _random;
    public readonly int NetId;

    public DeviceNet(int netId, IRobustRandom random)
    {
        _random = random;
        NetId = netId;
    }

    /// <summary>
    ///     Add a device to the network.
    /// </summary>
    public bool Add(DeviceNetworkComponent device)
    {
        if (device.CustomAddress)
        {
            // Only add if the device's existing address is available.
            if (!Devices.TryAdd(device.Address, device))
                return false;
        }
        else
        {
            // Randomly generate a new address if the existing random one is invalid. Otherwise, keep the existing address
            if (string.IsNullOrWhiteSpace(device.Address) || Devices.ContainsKey(device.Address))
                device.Address = GenerateValidAddress(device.Prefix);

            Devices[device.Address] = device;
        }

        if (device.ReceiveFrequency is not uint freq)
            return true;

        if (!ListeningDevices.TryGetValue(freq, out var devices))
            ListeningDevices[freq] = devices = new();

        devices.Add(device);

        if (!device.ReceiveAll)
            return true;

        if (!ReceiveAllDevices.TryGetValue(freq, out var receiveAlldevices))
            ReceiveAllDevices[freq] = receiveAlldevices = new();

        receiveAlldevices.Add(device);
        return true;
    }

    /// <summary>
    ///     Remove a device from the network.
    /// </summary>
    public bool Remove(DeviceNetworkComponent device)
    {
        if (device.Address == null || !Devices.Remove(device.Address))
            return false;

        if (device.ReceiveFrequency is not uint freq)
            return true;

        if (ListeningDevices.TryGetValue(freq, out var listening))
        {
            listening.Remove(device);
            if (listening.Count == 0)
                ListeningDevices.Remove(freq);
        }

        if (device.ReceiveAll && ReceiveAllDevices.TryGetValue(freq, out var receiveAll))
        {
            receiveAll.Remove(device);
            if (receiveAll.Count == 0)
                ListeningDevices.Remove(freq);
        }

        return true;
    }

    /// <summary>
    ///     Give an existing device a new randomly generated address. Useful if the device's address prefix was updated
    ///     and they want a new address to reflect that, or something like that.
    /// </summary>
    public bool RandomizeAddress(string oldAddress, string? prefix = null)
    {
        if (!Devices.Remove(oldAddress, out var device))
            return false;

        device.Address = GenerateValidAddress(prefix ?? device.Prefix);
        device.CustomAddress = false;
        Devices[device.Address] = device;
        return true;
    }

    /// <summary>
    ///     Update the address of an existing device.
    /// </summary>
    public bool UpdateAddress(string oldAddress, string newAddress)
    {
        if (Devices.ContainsKey(newAddress))
            return false;

        if (!Devices.Remove(oldAddress, out var device))
            return false;

        device.Address = newAddress;
        device.CustomAddress = true;
        Devices[newAddress] = device;
        return true;
    }

    /// <summary>
    ///     Make an existing network device listen to a new frequency.
    /// </summary>
    public bool UpdateReceiveFrequency(string address, uint? newFrequency)
    {
        if (!Devices.TryGetValue(address, out var device))
            return false;

        if (device.ReceiveFrequency == newFrequency)
            return true;

        if (device.ReceiveFrequency is uint freq)
        {
            if (ListeningDevices.TryGetValue(freq, out var listening))
            {
                listening.Remove(device);
                if (listening.Count == 0)
                    ListeningDevices.Remove(freq);
            }

            if (device.ReceiveAll && ReceiveAllDevices.TryGetValue(freq, out var receiveAll))
            {
                receiveAll.Remove(device);
                if (receiveAll.Count == 0)
                    ListeningDevices.Remove(freq);
            }
        }

        device.ReceiveFrequency = newFrequency;

        if (newFrequency == null)
            return true;

        if (!ListeningDevices.TryGetValue(newFrequency.Value, out var devices))
            ListeningDevices[newFrequency.Value] = devices = new();

        devices.Add(device);

        if (!device.ReceiveAll)
            return true;

        if (!ReceiveAllDevices.TryGetValue(newFrequency.Value, out var receiveAlldevices))
            ReceiveAllDevices[newFrequency.Value] = receiveAlldevices = new();

        receiveAlldevices.Add(device);
        return true;
    }

    /// <summary>
    ///     Make an existing network device listen to a new frequency.
    /// </summary>
    public bool UpdateReceiveAll(string address, bool receiveAll)
    {
        if (!Devices.TryGetValue(address, out var device))
            return false;

        if (device.ReceiveAll == receiveAll)
            return true;

        device.ReceiveAll = receiveAll;

        if (device.ReceiveFrequency is not uint freq)
            return true;

        // remove or add to set of listening devices

        HashSet<DeviceNetworkComponent>? devices;
        if (receiveAll)
        {
            if (!ReceiveAllDevices.TryGetValue(freq, out devices))
                ReceiveAllDevices[freq] = devices = new();
            devices.Add(device);
        }
        else if (ReceiveAllDevices.TryGetValue(freq, out devices))
        {
            devices.Remove(device);
            if (devices.Count == 0)
                ReceiveAllDevices.Remove(freq);
        }

        return true;
    }

    /// <summary>
    ///     Generates a valid address by randomly generating one and checking if it already exists on the network.
    /// </summary>
    private string GenerateValidAddress(string? prefix)
    {
        prefix = string.IsNullOrWhiteSpace(prefix) ? null : Loc.GetString(prefix);
        string address;
        do
            address = $"{prefix}{_random.Next():x}";
        while (Devices.ContainsKey(address));

        return address;
    }
}
