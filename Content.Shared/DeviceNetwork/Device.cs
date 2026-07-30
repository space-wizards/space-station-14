using Content.Shared.DeviceNetwork.Systems;

namespace Content.Shared.DeviceNetwork;

/// <summary>
/// Represents a device in a <see cref="DeviceNet"/>.
/// </summary>
/// <remarks>
/// This type is read-only. To change any parameters of the device, use <see cref="DeviceNetworkSystem"/>'s API.
/// </remarks>
[DataDefinition]
public readonly partial record struct Device(EntityUid Owner, DeviceData DeviceData)
{
    [DataField]
    public readonly EntityUid Owner = Owner;

    [IncludeDataField]
    public readonly DeviceData DeviceData = DeviceData;
}
