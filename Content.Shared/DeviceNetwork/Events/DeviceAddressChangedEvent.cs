namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Raised when the <see cref="DeviceData.Address"/> was changed.
/// </summary>
[ByRefEvent]
public record struct DeviceAddressChangedEvent(DeviceAddress OldAddress, DeviceAddress NewAddress, bool IsCustom);
