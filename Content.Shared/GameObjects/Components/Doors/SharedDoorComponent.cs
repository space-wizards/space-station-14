using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Doors
{
    [NetSerializable]
    [Serializable]
    public enum DoorVisuals : byte
    {
        VisualState,
        Powered,
        BoltLights
    }

    [NetSerializable]
    [Serializable]
    public enum DoorVisualState : byte
    {
        Closed,
        Opening,
        Open,
        Closing,
        Deny,
        Welded,
    }
}
