using System.Numerics;

namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Data about the device network packet that was received by another entity.
/// Doesn't include the actual <see cref="NetworkPayload"/> of the packet.
/// </summary>
public interface IDeviceNetworkPacket
{
    int NetId { get; set; }

    string? Address { get; set; }

    uint Frequency { get; set; }

    string SenderAddress { get; set; }

    EntityUid Sender { get; set; }
}

/// <inheritdoc cref="IDeviceNetworkPacket" />
public record struct DeviceNetworkPacketData(
    int NetId,
    string? Address,
    uint Frequency,
    string SenderAddress,
    EntityUid Sender,
    TransformComponent SenderTransform,
    Vector2 SenderPos) : IDeviceNetworkPacket;

[ByRefEvent]
public record struct DeviceNetworkPacketHandledEvent(
    int NetId,
    string? Address,
    uint Frequency,
    string SenderAddress,
    EntityUid Sender,
    HandledNetworkPayload Data) : IDeviceNetworkPacket;

/// <summary>
/// Event raised when a device network packet gets sent.
/// </summary>
[ByRefEvent]
public record struct DeviceNetworkPacketEvent(
    int NetId,
    string? Address,
    uint Frequency,
    string SenderAddress,
    EntityUid Sender,
    NetworkPayload Data) : IDeviceNetworkPacket;
