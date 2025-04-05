using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Visuals
{
    [Serializable, NetSerializable]
    public enum ThermomachineVisuals : byte
    {
        State,
    }

    [Serializable, NetSerializable]
    public enum ThermomachineState : byte
    {
        Off,
        On,
    }
}
