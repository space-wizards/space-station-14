namespace Content.Shared.DeviceNetwork.Events;

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
    NetworkPayload Data);
