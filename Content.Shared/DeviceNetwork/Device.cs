using Content.Shared.DeviceNetwork.Systems;

namespace Content.Shared.DeviceNetwork;

/// <summary>
/// Represents a device in a network.
/// </summary>
/// <remarks>
/// This type is read-only. To change any parameters of the device, use <see cref="DeviceNetworkSystem"/>'s API.
/// </remarks>
[DataDefinition]
public readonly partial struct Device(EntityUid owner, DeviceData deviceData) : IEquatable<Device>
{
    [DataField]
    public readonly EntityUid Owner = owner;

    [IncludeDataField]
    public readonly DeviceData DeviceData = deviceData;

    // Compares only for EntityUid and not the data
    public bool Equals(Device other)
    {
        return Owner.Equals(other.Owner);
    }

    public override bool Equals(object? obj)
    {
        return obj is Device other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Owner.GetHashCode();
    }

    public static bool operator ==(Device left, Device right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Device left, Device right)
    {
        return !left.Equals(right);
    }
}
