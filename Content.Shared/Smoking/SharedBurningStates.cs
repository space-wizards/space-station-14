#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    [Serializable, NetSerializable]
    public enum SharedBurningStates : byte
    {
        Unlit,
        Lit,
        Burnt,
    }
}
