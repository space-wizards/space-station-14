using Robust.Shared.Serialization;

namespace Content.Shared.Foam
{
    [Serializable, NetSerializable]
    public enum FoamVisuals : byte
    {
        State,
        Color
    }
}
