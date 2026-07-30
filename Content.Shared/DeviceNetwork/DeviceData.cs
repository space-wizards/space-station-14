using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork;

/// <summary>
/// Represents data about a networked device that is
/// capable of transmitting and receiving network packets.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial record struct DeviceData
{
    /// <summary>
    ///     The frequency that this device is listening on.
    /// </summary>
    [DataField]
    public DeviceFrequency? ReceiveFrequency;

    /// <summary>
    ///     The frequency that this device going to try transmit on.
    /// </summary>
    [DataField]
    public DeviceFrequency? TransmitFrequency;

    /// <summary>
    ///     The address ID of the device, either on the network it is currently connected to or whatever address it
    ///     most recently used.
    /// </summary>
    [IncludeDataField]
    public DeviceAddress AddressId;

    /// <summary>
    ///     If true, the address was customized and should be preserved across networks. If false, a randomly
    ///     generated address will be created whenever this device connects to a network.
    /// </summary>
    [DataField]
    public bool CustomAddress = false;

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
}
