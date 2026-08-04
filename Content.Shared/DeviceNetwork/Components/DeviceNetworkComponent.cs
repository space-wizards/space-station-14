using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceNetwork.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDeviceNetworkSystem), typeof(DeviceNet))]
public sealed partial class DeviceNetworkComponent : Component
{
    /// <summary>
    /// Default device network ID to connect to.
    /// </summary>
    [DataField("deviceNetId")]
    public DeviceNetIdDefaults NetIdEnum { get; set; }

    public int DeviceNetId => (int) NetIdEnum;

    /// <inheritdoc cref="DeviceData"/>
    [IncludeDataField, AutoNetworkedField]
    public DeviceData Data;

    /// <summary>
    /// The frequency that this device going to try to transmit on.
    /// </summary>
    [DataField]
    public uint? TransmitFrequency;

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
    [DataField]
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
    [DataField]
    public LocId? Prefix;

    /// <summary>
    /// Whether the device should attempt to join the network on map init.
    /// </summary>
    [DataField]
    public bool AutoConnect = true;

    /// <summary>
    /// Whether this device's address can be saved to device-lists
    /// </summary>
    [DataField]
    public bool SavableAddress = true;

    /// <summary>
    ///     If true, the address was customized and should be preserved across networks. If false, a randomly
    ///     generated address will be created whenever this device connects to a network.
    /// </summary>
    [DataField]
    public bool CustomAddress;

    /// <summary>
    /// A list of device-lists that this device is on.
    /// </summary>
    [DataField]
    [Access(typeof(SharedDeviceListSystem))]
    public HashSet<EntityUid> DeviceLists = new();

    /// <summary>
    /// A list of configurators that this device is on.
    /// </summary>
    [DataField]
    [Access(typeof(SharedNetworkConfiguratorSystem))]
    public HashSet<EntityUid> Configurators = new();

    #region Obsolete

    [Obsolete("Access this field through DeviceNetworkComponent.Data instead")]
    public uint? ReceiveFrequency
    {
        get => Data.ReceiveFrequency;
        set => Data.ReceiveFrequency = value;
    }

    [Obsolete("Access this field through DeviceNetworkComponent.Data instead")]
    public string Address
    {
        get => Data.Address;
        set => Data.Address = value;
    }

    [Obsolete("Access this field through DeviceNetworkComponent.Data instead")]
    public bool ReceiveAll
    {
        get => Data.ReceiveAll;
        set => Data.ReceiveAll = value;
    }

    #endregion
}
