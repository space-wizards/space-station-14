using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.DeviceNetwork.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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

    [IncludeDataField]
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
    [DataField]
    public bool SavableAddress = true;

    /// <summary>
    /// Amount of packets
    /// </summary>
    [DataField]
    public int PacketReceiveCap = 15;

    /// <summary>
    /// Amount of packets received in the current tick.
    /// If it gets higher than <see cref="PacketReceiveCap"/>, this device will overload.
    /// </summary>
    [ViewVariables]
    public int PacketReceiveCounter;

    /// <summary>
    /// Tick when the last packet was received.
    /// </summary>
    [ViewVariables]
    public GameTick LastPacketTick;

    /// <summary>
    /// Amount of time that device overload lasts.
    /// </summary>
    [DataField]
    public TimeSpan OverloadDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Time when the overload of the device ends.
    /// </summary>
    [DataField]
    public TimeSpan? OverloadEnd;

    [ViewVariables]
    public bool IsOverloaded => OverloadEnd.HasValue;

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
