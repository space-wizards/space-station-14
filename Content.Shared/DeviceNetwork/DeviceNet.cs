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
    public readonly Dictionary<int, Device> Devices = new();

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
}
