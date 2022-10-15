using Robust.Shared.Serialization;

namespace Content.Shared.Fluids
{
    [Serializable, NetSerializable]
    public enum PuddleVisuals : byte
    {
        VolumeScale,
        CurrentVolume,
        SolutionColor,
        IsEvaporatingVisual
    }
}
