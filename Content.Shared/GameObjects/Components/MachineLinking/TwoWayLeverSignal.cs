using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.MachineLinking
{
    [Serializable, NetSerializable]
    public enum TwoWayLeverVisuals
    {
        State
    }

    [Serializable, NetSerializable]
    public enum TwoWayLeverSignal
    {
        Middle,
        Left,
        Right
    }
}
