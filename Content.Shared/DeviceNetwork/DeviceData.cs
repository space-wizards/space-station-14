using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork;

/// <summary>
/// Represents data about a networked device that is
/// capable of receiving network packets.
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
    ///     The address ID of the device, either on the network it is currently connected to or whatever address it
    ///     most recently used.
    /// </summary>
    [DataField]
    public DeviceAddress AddressId;

    /// <summary>
    ///     Whether the device should listen for all device messages, regardless of the intended recipient.
    /// </summary>
    [DataField]
    public bool ReceiveAll;
}
