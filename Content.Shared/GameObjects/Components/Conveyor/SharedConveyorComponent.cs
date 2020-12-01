using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Conveyor
{
    [Serializable, NetSerializable]
    public enum ConveyorVisuals : byte
    {
        State
    }

    [Serializable, NetSerializable]
    public enum ConveyorState : byte
    {
        Off,
        Forward,
        Reversed
    }
}
