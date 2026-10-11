namespace Content.Shared.DeviceConfigurator;

/// <summary>
/// Event raised to update the devices stored in a device list.
/// </summary>
[ByRefEvent]
public readonly record struct DeviceListUpdateEvent(List<EntityUid> OldDevices, List<EntityUid> Devices);

/// <summary>
/// Result of attempting to update a device list.
/// </summary>
public enum DeviceListUpdateResult : byte
{
    /// <summary>The target entity does not have a device list component.</summary>
    NoComponent,

    /// <summary>The requested list exceeds its configured device limit.</summary>
    TooManyDevices,

    /// <summary>The device list was updated successfully.</summary>
    UpdateOk
}
