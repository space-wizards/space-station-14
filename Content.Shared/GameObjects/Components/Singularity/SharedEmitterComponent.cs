using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Singularity
{
    [NetSerializable, Serializable]
    public enum EmitterVisuals
    {
        VisualState,
        Locked
    }

    [NetSerializable, Serializable]
    public enum EmitterVisualState
    {
        On,
        Underpowered,
        Off
    }
}
