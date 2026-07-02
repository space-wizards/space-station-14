using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceNetwork.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDeviceNetworkSystem), typeof(DeviceNet))]
public sealed partial class DeviceNetworkComponent : Component, IDevice
{
    [DataField("deviceNetId")]
    public DeviceNetIdDefaults NetIdEnum { get; set; }

    public int DeviceNetId => (int) NetIdEnum;

    /// <summary>
    ///     The frequency that this device is listening on.
    /// </summary>
    [DataField]
    public uint? ReceiveFrequency { get; set; }

    /// <summary>
    ///     frequency prototype. Used to select a default frequency to listen to on. Used when the map is
    ///     initialized.
    /// </summary>
    [DataField]
    public ProtoId<DeviceFrequencyPrototype>? ReceiveFrequencyId;

    /// <summary>
    ///     The frequency that this device going to try transmit on.
    /// </summary>
    [DataField]
    public uint? TransmitFrequency { get; set; }

    /// <summary>
    ///     frequency prototype. Used to select a default frequency to transmit on. Used when the map is
    ///     initialized.
    /// </summary>
    [DataField]
    public ProtoId<DeviceFrequencyPrototype>? TransmitFrequencyId;

    /// <summary>
    ///     The address of the device, either on the network it is currently connected to or whatever address it
    ///     most recently used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    ///     If true, the address was customized and should be preserved across networks. If false, a randomly
    ///     generated address will be created whenever this device connects to a network.
    /// </summary>
    [DataField]
    public bool CustomAddress { get; set; }

    /// <summary>
    ///     Prefix to prepend to any automatically generated addresses. Helps players to identify devices.
    /// </summary>
    [DataField]
    public LocId? Prefix { get; set; }

    /// <summary>
    ///     Whether the device should listen for all device messages, regardless of the intended recipient.
    /// </summary>
    [DataField]
    public bool ReceiveAll { get; set; }

    /// <summary>
    ///     If the device should show its address upon an examine. Useful for devices
    ///     that do not have a visible UI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ExaminableAddress;

    /// <summary>
    ///     Whether the device should attempt to join the network on map init.
    /// </summary>
    [DataField]
    public bool AutoConnect = true;

    /// <summary>
    ///     Whether to send the broadcast recipients list to the sender so it can be filtered.
    /// </summary>
    [DataField]
    public bool SendBroadcastAttemptEvent { get; set; }

    /// <summary>
    ///     Whether this device's address can be saved to device-lists
    /// </summary>
    [DataField]
    public bool SavableAddress = true;

    /// <summary>
    ///     A list of device-lists that this device is on.
    /// </summary>
    [DataField]
    [Access(typeof(SharedDeviceListSystem))]
    public HashSet<EntityUid> DeviceLists = new();

    /// <summary>
    ///     A list of configurators that this device is on.
    /// </summary>
    [DataField]
    [Access(typeof(SharedNetworkConfiguratorSystem))]
    public HashSet<EntityUid> Configurators = new();
}
