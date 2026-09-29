using Content.Shared.DeviceNetwork.Components;

namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Event raised when a device network packet is received by an entity.
/// </summary>
/// <param name="NetId">
/// ID of the network that this packet is translated on.
/// Device networks are currently global and only represent the way of signal transmission.
/// </param>
/// <param name="Address">
/// Address of the target device in a target network.
/// If null, this packet gets broadcasted to all devices in the network.
/// Empty string means invalid address.
/// </param>
/// <param name="Frequency">
/// Transmit frequency of the sender and receive frequency of the targeted device.
/// </param>
/// <param name="SenderAddress">
/// The device address of the sender. Can be used to send responses to payloads.
/// </param>
/// <param name="Sender">
/// The sender entity with its <see cref="DeviceNetworkComponent"/>.
/// </param>
/// <param name="Data">
/// The <see cref="INetworkPayload"/> of a specific type.
/// This is the main container for custom information.
/// </param>
/// <typeparam name="T">Type of the payload sent by this event.</typeparam>
[ByRefEvent]
public readonly record struct DeviceNetworkPacketEvent<T>(
    int NetId,
    string? Address,
    uint Frequency,
    string SenderAddress,
    Entity<DeviceNetworkComponent> Sender,
    T Data) where T : INetworkPayload;
