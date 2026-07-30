using Robust.Shared.Random;
using Content.Shared.DeviceNetwork.Components;

namespace Content.Shared.DeviceNetwork;

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
    [ViewVariables]
    public readonly Dictionary<string, Device> Devices = new();

    /// <summary>
    ///     Devices listening on a given frequency.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<uint, HashSet<Device>> ListeningDevices = new();

    /// <summary>
    ///     Devices listening to all packets on a given frequency, regardless of the intended recipient.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<uint, HashSet<Device>> ReceiveAllDevices = new();

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
    public bool Add(Entity<DeviceNetworkComponent> ent)
    {
        var deviceComp = ent.Comp;
        var device = new Device(ent.Owner, ent.Comp.Data);
        if (deviceComp.Data.CustomAddress)
        {
            // Only add if the device's existing address is available.
            if (!Devices.TryAdd(deviceComp.Data.Address, device))
                return false;
        }
        else
        {
            // Randomly generate a new address if the existing random one is invalid. Otherwise, keep the existing address
            if (string.IsNullOrWhiteSpace(deviceComp.Data.Address) || Devices.ContainsKey(deviceComp.Data.Address))
            {
                deviceComp.Data.Address = GenerateValidAddress(deviceComp.Prefix);
                device = new Device(ent.Owner, ent.Comp.Data); // Reallocate because the data had changed
            }

            Devices[deviceComp.Data.Address] = device;
        }

        if (deviceComp.Data.ReceiveFrequency is not { } freq)
            return true;

        if (!ListeningDevices.TryGetValue(freq, out var devices))
            ListeningDevices[freq] = devices = new();

        devices.Add(device);

        if (!deviceComp.Data.ReceiveAll)
            return true;

        if (!ReceiveAllDevices.TryGetValue(freq, out var receiveAlldevices))
            ReceiveAllDevices[freq] = receiveAlldevices = new();

        receiveAlldevices.Add(device);
        return true;
    }

    /// <summary>
    ///     Remove a device from the network.
    /// </summary>
    public bool Remove(Entity<DeviceNetworkComponent> ent)
    {
        var deviceComp = ent.Comp;
        var device = new Device(ent.Owner, ent.Comp.Data);
        if (!Devices.Remove(deviceComp.Data.Address))
            return false;

        if (deviceComp.Data.ReceiveFrequency is not { } freq)
            return true;

        if (ListeningDevices.TryGetValue(freq, out var listening))
        {
            listening.Remove(device);
            if (listening.Count == 0)
                ListeningDevices.Remove(freq);
        }

        if (deviceComp.Data.ReceiveAll && ReceiveAllDevices.TryGetValue(freq, out var receiveAll))
        {
            receiveAll.Remove(device);
            if (receiveAll.Count == 0)
                ListeningDevices.Remove(freq);
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
        {
            var num = _random.Next();
            address = $"{prefix}{num >> 16:X4}-{num & 0xFFFF:X4}";
        } while (Devices.ContainsKey(address));

        return address;
    }
}
