using Robust.Shared.Serialization;

namespace Content.Shared.Conveyor
{
    [Serializable, NetSerializable]
    public enum ConveyorVisuals
    {
        State
    }

    [Serializable, NetSerializable]
    public enum ConveyorState
    {
        Off,
        Forward,
        Reverse
    }
}
