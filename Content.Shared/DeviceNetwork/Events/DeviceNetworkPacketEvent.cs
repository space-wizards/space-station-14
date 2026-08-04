using Content.Shared.DeviceNetwork.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Event raised when a device network packet gets sent.
/// </summary>
[ByRefEvent]
public readonly record struct DeviceNetworkPacketEvent<T>(
    ProtoId<DeviceNetworkPrototype> NetId,
    DeviceAddress? Address,
    DeviceFrequency Frequency,
    DeviceAddress SenderAddress,
    Entity<DeviceNetworkComponent> Sender,
    T Data) where T : INetworkPayload;

/// <summary>
/// A helper struct that contains the same data as <see cref="DeviceNetworkPacketEvent{T}"/> but without the payload itself.
/// DO NOT use this unless you know what you're doing!
/// </summary>
[ByRefEvent]
public record struct DeviceNetworkPacketData(
    ProtoId<DeviceNetworkPrototype> NetId,
    DeviceAddress? Address,
    DeviceFrequency Frequency,
    DeviceAddress SenderAddress,
    Entity<DeviceNetworkComponent> Sender,
    INetworkPayload Data);
