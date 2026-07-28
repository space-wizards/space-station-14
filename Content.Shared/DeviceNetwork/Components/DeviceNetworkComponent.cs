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
    [IncludeDataField]
    public DeviceData Data;

    /// <summary>
    /// Frequency prototype. Used to select a default frequency to listen to on. Used when the map is
    /// initialized.
    /// </summary>
    [DataField]
    public ProtoId<DeviceFrequencyPrototype>? ReceiveFrequencyId;

    /// <summary>
    /// Frequency prototype. Used to select a default frequency to transmit on. Used when the map is
    /// initialized.
    /// </summary>
    [DataField]
    public ProtoId<DeviceFrequencyPrototype>? TransmitFrequencyId;

    /// <summary>
    /// If the device should show its address upon an examine.
    /// Useful for devices that do not have a visible UI.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ExaminableAddress;

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
    public uint? TransmitFrequency
    {
        get => Data.TransmitFrequency;
        set => Data.TransmitFrequency = value;
    }

    [Obsolete("Access this field through DeviceNetworkComponent.Data instead")]
    public string Address
    {
        get => Data.Address;
        set => Data.Address = value;
    }

    [Obsolete("Access this field through DeviceNetworkComponent.Data instead")]
    public bool CustomAddress
    {
        get => Data.CustomAddress;
        set => Data.CustomAddress = value;
    }

    [Obsolete("Access this field through DeviceNetworkComponent.Data instead")]
    public bool ReceiveAll
    {
        get => Data.ReceiveAll;
        set => Data.ReceiveAll = value;
    }

    [Obsolete("Access this field through DeviceNetworkComponent.Data instead")]
    public bool SendBroadcastAttemptEvent
    {
        get => Data.SendBroadcastAttemptEvent;
        set => Data.SendBroadcastAttemptEvent = value;
    }

    [Obsolete("Access this field through DeviceNetworkComponent.Data instead")]
    public LocId? Prefix
    {
        get => Data.Prefix;
        set => Data.Prefix = value;
    }

    #endregion
}
