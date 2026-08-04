using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceNetwork.Components.Networks;

/// <summary>
///     Data class for storing and retrieving information about devices connected to a device network.
/// </summary>
/// <remarks>
///     This basically just makes <see cref="DeviceNetworkComponent"/> accessible via their addresses and frequencies on
///     some network.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DeviceNetworkManagerComponent : Component
{
    /// <summary>
    /// Network ID that this entity is managing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<DeviceNetworkPrototype>? DeviceNetId;

    /// <summary>
    ///     Devices, mapped by their "Address", which is just an int that gets converted to Hex for displaying to users.
    ///     This dictionary contains all devices connected to this network, though they may not be listening to any
    ///     specific frequency.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<DeviceAddress, Device> Devices = new();

    /// <summary>
    ///     Devices listening on a given frequency.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<DeviceFrequency, HashSet<Device>> ListeningDevices = new();

    /// <summary>
    ///     Devices listening to all packets on a given frequency, regardless of the intended recipient.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<DeviceFrequency, HashSet<Device>> ReceiveAllDevices = new();
}
