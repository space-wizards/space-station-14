using Content.Shared.DeviceNetwork.Components;

namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Raised when the <see cref="DeviceNetworkComponent.TransmitFrequency"/> was changed.
/// </summary>
[ByRefEvent]
public record struct DeviceTransmitFrequencyChangedEvent(uint? OldFrequency, uint? NewFrequency);
