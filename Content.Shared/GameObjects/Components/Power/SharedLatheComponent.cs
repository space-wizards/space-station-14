#nullable enable
using System;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Power
{
    [Serializable, NetSerializable]
    public enum LatheVisualData
    {
        State,
        Powered,
        Broken,
        Color
    }
    [Serializable, NetSerializable]
    public enum LatheVisualState
    {
        Idle,
        Producing,
        Inserting,
    }
}
