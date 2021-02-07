using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Doors
{
    [NetSerializable]
    [Serializable]
    public enum DoorVisuals
    {
        VisualState,
        Powered,
        BoltLights
    }

    [NetSerializable]
    [Serializable]
    public enum DoorVisualState
    {
        Closed,
        Opening,
        Open,
        Closing,
        Deny,
        Welded,
    }
}
