namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Event raised when a device network packet gets sent.
/// </summary>
[ByRefEvent]
public record struct DeviceNetworkPacketEvent<T>(
    int NetId,
    string? Address,
    uint Frequency,
    string SenderAddress,
    EntityUid Sender,
    T Data) where T : NetworkPayloadBase<T>;

/// <summary>
/// A wrapper for <see cref="DeviceNetworkPacketEvent{T}"/> without the typed parameter.
/// </summary>
[ByRefEvent]
public record struct DeviceNetworkPacketData(
    int NetId,
    string? Address,
    uint Frequency,
    string SenderAddress,
    EntityUid Sender,
    INetworkPayload Data);
