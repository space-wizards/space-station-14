using Robust.Shared.Serialization;

namespace Content.Shared.Pinpointer
{
    [Serializable, NetSerializable]
    public enum PinpointerVisuals : byte
    {
        IsActive,
        ArrowAngle,
        TargetDistance
    }

    public enum PinpointerLayers : byte
    {
        Base,
        Screen
    }
}
