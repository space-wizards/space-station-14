using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Fluids
{
    [Serializable, NetSerializable]
    public enum PuddleVisual : byte
    {
        VolumeScale,
        SolutionColor
    }
}
