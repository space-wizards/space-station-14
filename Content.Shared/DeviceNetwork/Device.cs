namespace Content.Shared.DeviceNetwork;

/// <summary>
/// Represents a device in a <see cref="DeviceNet"/>.
/// </summary>
[DataDefinition]
public partial record struct Device
{
    [DataField]
    public EntityUid Owner;

    [IncludeDataField]
    public DeviceData DeviceData;

    public Device(EntityUid uid, DeviceData data)
    {
        Owner = uid;
        DeviceData = data;
    }
}
