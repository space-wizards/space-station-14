using System;

namespace Content.Shared.GameObjects.Components.Damage
{
    [Flags]
    public enum DamageFlag
    {
        None = 0,
        Invulnerable = 1 << 0
    }
}
