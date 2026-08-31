namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Raised when the <see cref="DeviceData.ReceiveFrequency"/> was changed.
/// </summary>
[ByRefEvent]
public record struct DeviceReceiveFrequencyChangedEvent(uint? OldFrequency, uint? NewFrequency);
