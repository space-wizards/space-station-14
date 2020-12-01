using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Singularity
{
    [NetSerializable, Serializable]
    public enum EmitterVisuals : byte
    {
        VisualState,
        Locked
    }

    [NetSerializable, Serializable]
    public enum EmitterVisualState : byte
    {
        On,
        Underpowered,
        Off
    }
}
