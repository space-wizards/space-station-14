using Content.Shared.DeviceNetwork.Components;

namespace Content.Shared.DeviceNetwork;

/// <summary>
/// Represents a device in a DeviceNet.
/// </summary>
[DataDefinition]
public partial record struct Device
{
    [DataField]
    public EntityUid DeviceOwner;

    [DataField]
    public DeviceNetIdDefaults NetIdEnum { get; set; }

    /// <summary>
    ///     The frequency that this device is listening on.
    /// </summary>
    [DataField]
    public uint? ReceiveFrequency;

    /// <summary>
    ///     The frequency that this device going to try transmit on.
    /// </summary>
    [DataField]
    public uint? TransmitFrequency;

    /// <summary>
    ///     The address of the device, either on the network it is currently connected to or whatever address it
    ///     most recently used.
    /// </summary>
    [DataField]
    public string Address = string.Empty;

    /// <summary>
    ///     If true, the address was customized and should be preserved across networks. If false, a randomly
    ///     generated address will be created whenever this device connects to a network.
    /// </summary>
    [DataField]
    public bool CustomAddress = false;

    /// <summary>
    ///     Prefix to prepend to any automatically generated addresses. Helps players to identify devices. This gets
    ///     localized.
    /// </summary>
    [DataField]
    public string? Prefix;

    /// <summary>
    ///     Whether the device should listen for all device messages, regardless of the intended recipient.
    /// </summary>
    [DataField]
    public bool ReceiveAll;

    /// <summary>
    ///     Whether to send the broadcast recipients list to the sender so it can be filtered.
    /// </summary>
    [DataField]
    public bool SendBroadcastAttemptEvent = false;

    public Device(Entity<DeviceNetworkComponent> ent)
    {
        DeviceOwner = ent.Owner;
        NetIdEnum = ent.Comp.NetIdEnum;
        ReceiveFrequency = ent.Comp.ReceiveFrequency;
        TransmitFrequency = ent.Comp.TransmitFrequency;
        Address = ent.Comp.Address;
        CustomAddress = ent.Comp.CustomAddress;
        Prefix = ent.Comp.Prefix;
        ReceiveAll = ent.Comp.ReceiveAll;
        SendBroadcastAttemptEvent = ent.Comp.SendBroadcastAttemptEvent;
    }

    public Device(EntityUid uid, DeviceNetworkComponent comp)
    {
        DeviceOwner = uid;
        NetIdEnum = comp.NetIdEnum;
        ReceiveFrequency = comp.ReceiveFrequency;
        TransmitFrequency = comp.TransmitFrequency;
        Address = comp.Address;
        CustomAddress = comp.CustomAddress;
        Prefix = comp.Prefix;
        ReceiveAll = comp.ReceiveAll;
        SendBroadcastAttemptEvent = comp.SendBroadcastAttemptEvent;
    }
}
