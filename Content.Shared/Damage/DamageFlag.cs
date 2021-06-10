#nullable enable
using System;
using Robust.Shared.Serialization;

namespace Content.Shared.Damage
{
    [Flags]
    [Serializable, NetSerializable]
    public enum DamageFlag
    {
        None = 0,
        Invulnerable = 1 << 0
    }
}
