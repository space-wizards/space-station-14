using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Conveyor
{
    [Serializable, NetSerializable]
    public enum ConveyorVisuals
    {
        State
    }

    [Serializable, NetSerializable]
    public enum ConveyorState
    {
        Stopped = 0,
        Running,
        Reversed
    }
}
