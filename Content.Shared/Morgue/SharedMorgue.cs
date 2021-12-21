using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Morgue
{
    [Serializable, NetSerializable]
    public enum MorgueVisuals
    {
        Open,
        HasContents,
        HasMob,
        HasSoul,
    }

    [Serializable, NetSerializable]
    public enum CrematoriumVisuals
    {
        Burning,
    }
}
