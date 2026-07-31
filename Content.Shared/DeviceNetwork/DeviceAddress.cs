using Content.Shared.DeviceNetwork.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork;

/// <summary>
/// A wrapper struct that represents a device address.
/// </summary>
[DataRecord, Serializable, NetSerializable]
public readonly partial record struct DeviceAddress(int AddressId)
{
    public static readonly DeviceAddress Invalid = new(0);

    public static implicit operator int(DeviceAddress address)
    {
        return address.AddressId;
    }

    public static implicit operator DeviceAddress(int addressId)
    {
        return new DeviceAddress(addressId);
    }

    public static implicit operator DeviceAddress(LocDeviceAddress address)
    {
        return address.AddressId;
    }

    public bool IsValid()
    {
        return AddressId != 0;
    }
}

/// <summary>
/// A pair of a <see cref="DeviceAddress"/> and its locale prefix.
/// This is enough to fully reconstruct a full string of this device.
/// </summary>
[DataRecord, Serializable, NetSerializable]
public readonly partial record struct LocDeviceAddress(DeviceAddress AddressId, LocId? Prefix)
{
    public static implicit operator LocDeviceAddress((DeviceAddress AddressId, LocId? Prefix) tuple)
    {
        return new LocDeviceAddress(tuple.AddressId, tuple.Prefix);
    }

    public static implicit operator LocDeviceAddress(DeviceNetworkComponent component)
    {
        return new LocDeviceAddress(component.Data.AddressId, component.Prefix);
    }

    public bool Equals(LocDeviceAddress? other)
    {
        return other != null && AddressId == other.Value.AddressId;
    }

    public override string ToString()
    {
        return DeviceLocalizationHelpers.GetAddressFromId(AddressId, Prefix);
    }
}
