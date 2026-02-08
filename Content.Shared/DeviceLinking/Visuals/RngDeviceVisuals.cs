using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking.Visuals;

/// <summary>
/// Used to determine which visuals to update for RNG devices.
/// </summary>
[Serializable, NetSerializable]
public enum RngDeviceVisuals
{
    State,
    Roll
}
