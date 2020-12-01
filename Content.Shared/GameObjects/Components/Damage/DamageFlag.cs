using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Damage
{
    [Flags]
    [Serializable, NetSerializable]
    public enum DamageFlag : byte
    {
        None = 0,
        Invulnerable = 1 << 0
    }
}
