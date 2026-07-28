namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Raised when the <see cref="DeviceData.ReceiveAll"/> was changed.
/// </summary>
[ByRefEvent]
public record struct DeviceReceiveAllChangedEvent(bool ReceiveAll);
