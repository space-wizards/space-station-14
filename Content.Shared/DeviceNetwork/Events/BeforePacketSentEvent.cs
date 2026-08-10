using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Event raised before a device network packet is sent.
/// Subscribed to by other systems to prevent the packet from being sent.
/// </summary>
/// <remarks>
/// It's recommended to make a new device network type to add and remove the entity from it on some event subscriptions.
/// </remarks>
[ByRefEvent]
public record struct BeforePacketSentEvent(
    ProtoId<DeviceNetworkPrototype> NetId,
    DeviceAddress? Address,
    DeviceFrequency Frequency,
    DeviceAddress SenderAddress,
    EntityUid Sender,
    TransformComponent SenderTransform,
    Vector2 SenderPosition,
    bool Cancelled = false);
