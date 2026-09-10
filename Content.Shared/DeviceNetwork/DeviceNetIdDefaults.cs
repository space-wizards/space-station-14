using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork;

[Serializable, NetSerializable]
public enum DeviceNetIdDefaults
{
    Private,
    Wired,
    Wireless,
    Apc,
    AtmosDevices,
    Reserved = 100,
    // Ids outside this enum may exist
    // This exists to let yml use nice names instead of numbers
}
