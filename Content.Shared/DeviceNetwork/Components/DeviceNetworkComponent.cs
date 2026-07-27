using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceNetwork.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(SharedDeviceNetworkSystem), typeof(DeviceNet))]
public sealed partial class DeviceNetworkComponent : Component
{
    public int DeviceNetId => (int) Data.NetIdEnum;

    [ViewVariables]
    public uint? ReceiveFrequency
    {
        get => Data.ReceiveFrequency;
        set => Data.ReceiveFrequency = value;
    }

    [ViewVariables]
    public uint? TransmitFrequency
    {
        get => Data.TransmitFrequency;
        set => Data.TransmitFrequency = value;
    }

    [ViewVariables]
    public string Address
    {
        get => Data.Address;
        set => Data.Address = value;
    }

    [ViewVariables]
    public bool CustomAddress
    {
        get => Data.CustomAddress;
        set => Data.CustomAddress = value;
    }

    [ViewVariables]
    public bool ReceiveAll
    {
        get => Data.ReceiveAll;
        set => Data.ReceiveAll = value;
    }

    [ViewVariables]
    public bool SendBroadcastAttemptEvent
    {
        get => Data.SendBroadcastAttemptEvent;
        set => Data.SendBroadcastAttemptEvent = value;
    }

    [ViewVariables]
    public LocId? Prefix
    {
        get => Data.Prefix;
        set => Data.Prefix = value;
    }

    [IncludeDataField, AutoNetworkedField]
    public DeviceData Data;

    /// <summary>
    ///     frequency prototype. Used to select a default frequency to listen to on. Used when the map is
    ///     initialized.
    /// </summary>
    [DataField]
    public ProtoId<DeviceFrequencyPrototype>? ReceiveFrequencyId;

    /// <summary>
    ///     frequency prototype. Used to select a default frequency to transmit on. Used when the map is
    ///     initialized.
    /// </summary>
    [DataField]
    public ProtoId<DeviceFrequencyPrototype>? TransmitFrequencyId;

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
    ///     Whether this device's address can be saved to device-lists
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SavableAddress = true;

    /// <summary>
    ///     A list of device-lists that this device is on.
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(typeof(DeviceListSystem))]
    public HashSet<EntityUid> DeviceLists = new();

    /// <summary>
    ///     A list of configurators that this device is on.
    /// </summary>
    [DataField, AutoNetworkedField]
    [Access(typeof(NetworkConfiguratorSystem))]
    public HashSet<EntityUid> Configurators = new();
}
