#nullable enable
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
        Off,
        Forward,
        Reversed
    }
}
