using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceNetwork.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(DeviceNetworkSystem), typeof(DeviceNet))]
public sealed partial class DeviceNetworkComponent : Component
{
    /// <summary>
    /// Default device network ID to connect to.
    /// </summary>
    [DataField("deviceNetId")]
    public DeviceNetIdDefaults NetIdEnum { get; set; }

    public int DeviceNetId => (int) NetIdEnum;

    public LocDeviceAddress LocAddress => new(Address, Prefix);

    /// <summary>
    ///     The frequency that this device is listening on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DeviceFrequency? ReceiveFrequency;

    /// <summary>
    ///     The address ID of the device, either on the network it is currently connected to or whatever address it
    ///     most recently used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DeviceAddress Address = DeviceAddress.Invalid;

    /// <summary>
    ///     Whether the device should listen for all device messages, regardless of the intended recipient.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ReceiveAll;

    /// <summary>
    /// The frequency that this device going to try to transmit on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DeviceFrequency? TransmitFrequency;

    /// <summary>
    /// Frequency prototype, used to select a default frequency to listen to on.
    /// </summary>
    [DataField]
    public ProtoId<DeviceFrequencyPrototype>? ReceiveFrequencyId;

    /// <summary>
    /// Frequency prototype, used to select a default frequency to transmit on.
    /// </summary>
    [DataField]
    public ProtoId<DeviceFrequencyPrototype>? TransmitFrequencyId;

    /// <summary>
    /// Whether to send the broadcast recipients list to the sender so it can be filtered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SendBroadcastAttemptEvent;

    /// <summary>
    /// If the device should show its address upon an examine.
    /// Useful for devices that do not have a visible UI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ExaminableAddress;

    /// <summary>
    /// Prefix to prepend to any automatically generated addresses. Helps players to identify devices.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? Prefix;

    /// <summary>
    /// Whether the device should attempt to join the network on map init.
    /// </summary>
    [DataField]
    public bool AutoConnect = true;

    /// <summary>
    /// Whether this device's address can be saved to device-lists
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SavableAddress = true;

    /// <summary>
    ///     If true, the address was customized and should be preserved across networks. If false, a randomly
    ///     generated address will be created whenever this device connects to a network.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CustomAddress;
}
