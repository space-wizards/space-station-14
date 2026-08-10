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

    /// <summary>
    /// Converts the address into its HEX representation.
    /// </summary>
    /// <remarks>
    /// Use this carefully, it's recommended to use <see cref="LocDeviceAddress"/> in general cases.
    /// </remarks>
    public override string ToString()
    {
        return $"{AddressId >> 16:X4}-{AddressId & 0xFFFF:X4}";
    }
}

/// <summary>
/// A pair of a <see cref="DeviceAddress"/> and its locale prefix.
/// This is enough to fully reconstruct a full string of this device.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public readonly partial struct LocDeviceAddress(DeviceAddress addressId, LocId? prefix) : IEquatable<DeviceAddress>, IEquatable<LocDeviceAddress>
{
    [DataField]
    public readonly DeviceAddress AddressId = addressId;

    [DataField]
    public readonly LocId? Prefix = prefix;

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
        var prefix = string.IsNullOrWhiteSpace(Prefix) ? null : Loc.GetString(Prefix);
        return $"{prefix}{AddressId.ToString()}";
    }

    public bool Equals(LocDeviceAddress other)
    {
        return AddressId.Equals(other.AddressId) && Nullable.Equals(Prefix, other.Prefix);
    }

    public bool Equals(DeviceAddress other)
    {
        return AddressId.Equals(other);
    }

    public override bool Equals(object? obj)
    {
        return obj is LocDeviceAddress other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(AddressId, Prefix);
    }

    public static bool operator ==(LocDeviceAddress left, LocDeviceAddress right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LocDeviceAddress left, LocDeviceAddress right)
    {
        return !left.Equals(right);
    }
}
