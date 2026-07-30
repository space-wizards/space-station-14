using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork;

/// <summary>
/// A wrapper struct that represents a device frequency.
/// </summary>
[DataRecord, Serializable, NetSerializable]
public readonly partial record struct DeviceFrequency(ushort FrequencyId)
{
    public static implicit operator ushort(DeviceFrequency frequency)
    {
        return frequency.FrequencyId;
    }

    public static implicit operator DeviceFrequency(ushort frequencyId)
    {
        return new DeviceFrequency(frequencyId);
    }
}
