using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Smoking
{
    [Serializable, NetSerializable]
    public enum SharedBurningStates : byte
    {
        Unlit,
        Lit,
        Burnt,
    }
}
