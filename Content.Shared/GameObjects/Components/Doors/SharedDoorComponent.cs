using System;
using SS14.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Doors
{
    [NetSerializable]
    [Serializable]
    public enum DoorVisuals
    {
        VisualState,
    }

    [NetSerializable]
    [Serializable]
    public enum DoorVisualState
    {
        Closed,
        Opening,
        Open,
        Closing,
    }
}
