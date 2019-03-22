using System;
using SS14.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged
{
    [Serializable, NetSerializable]
    public enum BallisticMagazineWeaponVisuals
    {
        MagazineLoaded,
        AmmoCapacity,
        AmmoLeft,
    }
}
