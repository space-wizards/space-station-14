using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Light
{
    [Serializable, NetSerializable]
    public enum CandleState
    {
        BrandNew,
        Half,
        AlmostOut,
        Dead
    }
}
