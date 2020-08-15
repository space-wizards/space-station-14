using System;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged
{
    public abstract class SharedRangedBarrelComponent : Component
    {
        public abstract FireRateSelector FireRateSelector { get; }
        public abstract FireRateSelector AllRateSelectors { get; }
        public abstract float FireRate { get; }
        public abstract int ShotsLeft { get; }
        public abstract int Capacity { get; }
    }

    [Flags]
    public enum FireRateSelector
    {
        Safety = 0,
        Single = 1 << 0,
        Automatic = 1 << 1,
    }
}