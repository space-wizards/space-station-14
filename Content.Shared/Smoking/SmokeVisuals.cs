using Robust.Shared.Serialization;

namespace Content.Shared.Smoking
{
    [Serializable, NetSerializable]
    public enum SmokeVisuals : byte
    {
        Color
    }

    [Serializable, NetSerializable]
    public enum FoamVisuals : byte
    {
        State,
        Color,
    }
}
