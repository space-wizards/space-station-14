using System;
using Robust.Shared.Serialization;

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
