namespace Content.Shared.DeviceNetwork;

/// <summary>
/// General data about a device in the device network.
/// </summary>
public interface IDevice
{
    DeviceNetIdDefaults NetIdEnum { get; set; }

    /// <summary>
    ///     The frequency that this device is listening on.
    /// </summary>
    uint? ReceiveFrequency { get; set; }

    /// <summary>
    ///     The frequency that this device going to try transmit on.
    /// </summary>
    uint? TransmitFrequency { get; set; }

    /// <summary>
    ///     The address of the device, either on the network it is currently connected to or whatever address it
    ///     most recently used.
    /// </summary>
    string Address { get; set; }

    /// <summary>
    ///     If true, the address was customized and should be preserved across networks. If false, a randomly
    ///     generated address will be created whenever this device connects to a network.
    /// </summary>
    bool CustomAddress { get; set; }

    /// <summary>
    ///     Prefix to prepend to any automatically generated addresses. Helps players to identify devices. This gets
    ///     localized.
    /// </summary>
    LocId? Prefix { get; set; }

    /// <summary>
    ///     Whether the device should listen for all device messages, regardless of the intended recipient.
    /// </summary>
    bool ReceiveAll { get; set; }

    /// <summary>
    ///     Whether to send the broadcast recipients list to the sender so it can be filtered.
    /// </summary>
    bool SendBroadcastAttemptEvent { get; set; }
}

