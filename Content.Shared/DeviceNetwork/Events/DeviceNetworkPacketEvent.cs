namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Event raised when a device network packet gets sent.
/// </summary>
[ByRefEvent]
public readonly record struct DeviceNetworkPacketEvent<T>(
    int NetId,
    string? Address,
    uint Frequency,
    string SenderAddress,
    EntityUid Sender,
    T Data) where T : INetworkPayload;

/// <summary>
/// A helper struct that contains the same data as <see cref="DeviceNetworkPacketEvent{T}"/> but without the payload itself.
/// </summary>
[ByRefEvent]
public record struct DeviceNetworkPacketData(
    int NetId,
    string? Address,
    uint Frequency,
    string SenderAddress,
    EntityUid Sender,
    INetworkPayload Data);
