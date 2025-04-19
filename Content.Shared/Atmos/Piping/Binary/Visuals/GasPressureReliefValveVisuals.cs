using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Binary.Visuals;

[Serializable, NetSerializable]
public enum PressureReliefValveVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum PressureReliefValveState : byte
{
    Off,
    On,
}
