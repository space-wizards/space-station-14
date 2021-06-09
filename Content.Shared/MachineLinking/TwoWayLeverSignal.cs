#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.MachineLinking
{
    [Serializable, NetSerializable]
    public enum TwoWayLeverVisuals : byte
    {
        State
    }

    [Serializable, NetSerializable]
    public enum TwoWayLeverSignal : byte
    {
        Middle,
        Left,
        Right
    }
}
