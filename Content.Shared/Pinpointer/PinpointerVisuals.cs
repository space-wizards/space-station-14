using Robust.Shared.Serialization;
using System;

namespace Content.Shared.Pinpointer
{
    [Serializable, NetSerializable]
    public enum PinpointerVisuals : byte
    {
        IsActive,
        TargetDirection
    }

    public enum PinpointerLayers : byte
    {
        Base,
        Screen
    }
}
