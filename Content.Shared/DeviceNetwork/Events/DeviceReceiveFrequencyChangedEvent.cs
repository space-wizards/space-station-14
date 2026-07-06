using Content.Shared.DeviceNetwork.Components;

namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Raised when the <see cref="DeviceNetworkComponent.ReceiveFrequency"/> was changed.
/// </summary>
[ByRefEvent]
public record struct DeviceReceiveFrequencyChangedEvent(uint? OldFrequency, uint? NewFrequency);
