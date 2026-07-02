using System.Numerics;

namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Event raised before a device network packet is sent.
/// Subscribed to by other systems to prevent the packet from being sent.
/// </summary>
[ByRefEvent]
public record struct BeforePacketSentEvent(
    int NetId,
    string? Address,
    uint Frequency,
    string SenderAddress,
    EntityUid Sender,
    TransformComponent SenderTransform,
    Vector2 SenderPosition,
    bool Cancelled = false) : IDeviceNetworkPacket;
