using System.Diagnostics.CodeAnalysis;
using Content.Shared.DeviceConfigurator.Systems;
using Content.Shared.DeviceNetwork;

namespace Content.Shared.DeviceLinking;

/// <summary>
/// <para>
/// An option helper struct for <see cref="DeviceListSystem"/> API that holds either a real address,
/// or a UID identifier to distinguish entities.
/// </para>
/// <para>
/// If the entity is map-init, holds the device address
/// </para>
/// </summary>
/// <remarks>
/// TODO .NET 11: This should probably be replaced with a real Option or Result type
/// </remarks>
public readonly struct OptionDeviceAddress
{
    private readonly LocDeviceAddress? _address;
    public readonly EntityUid Uid;

    public OptionDeviceAddress(LocDeviceAddress address, EntityUid entity)
    {
        _address = address;
        Uid = entity;
    }

    public OptionDeviceAddress(EntityUid entity)
    {
        _address = null;
        Uid = entity;
    }

    public bool TryGetAddress([NotNullWhen(true)] out LocDeviceAddress? address)
    {
        address = _address;
        return address != null;
    }
}
