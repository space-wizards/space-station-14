using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Visuals
{
    [Serializable, NetSerializable]
    public enum GasVolumePumpVisuals : byte
    {
        State,
    }

    [Serializable, NetSerializable]
    public enum GasVolumePumpState : byte
    {
        Off,
        On,
        Blocked,
    }
}
