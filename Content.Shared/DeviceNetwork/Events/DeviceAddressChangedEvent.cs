namespace Content.Shared.DeviceNetwork.Events;

/// <summary>
/// Raised when the <see cref="DeviceData.AddressId"/> was changed.
/// </summary>
[ByRefEvent]
public readonly record struct DeviceAddressChangedEvent(
    int OldAddress,
    int NewAddress,
    LocId? OldPrefix,
    LocId? NewPrefix,
    bool IsCustom);
