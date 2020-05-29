using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels
{
    [Serializable, NetSerializable]
    public enum BatteryBarrelVisuals
    {
        AmmoCount,
        AmmoMax,
        BatteryLoaded,
    }
}