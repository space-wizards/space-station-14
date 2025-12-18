using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Visuals
{
    [Serializable, NetSerializable]
    public enum VentPumpVisuals : byte
    {
        State,
    }

    [Serializable, NetSerializable]
    public enum VentPumpState : byte
    {
        Off,
        In,
        Out,
        Welded,
        Lockout,
    }
}
