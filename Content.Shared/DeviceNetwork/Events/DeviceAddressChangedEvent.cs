namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Raised when the <see cref="DeviceData.Address"/> was changed.
/// </summary>
[ByRefEvent]
public record struct DeviceAddressChangedEvent(string OldAddress, string NewAddress, bool IsCustom);
