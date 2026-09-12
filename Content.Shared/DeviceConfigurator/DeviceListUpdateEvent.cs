namespace Content.Shared.DeviceConfigurator;

[ByRefEvent]
public readonly record struct DeviceListUpdateEvent(List<EntityUid> OldDevices, List<EntityUid> Devices);

public enum DeviceListUpdateResult : byte
{
    NoComponent,
    TooManyDevices,
    UpdateOk
}
