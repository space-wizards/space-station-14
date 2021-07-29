using System;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Barrels.Components
{
    [Serializable, NetSerializable]
    public class BatteryBarrelComponentState : ComponentState
    {
        public FireRateSelector FireRateSelector { get; }
        public (int count, int max)? Magazine { get; }

        public BatteryBarrelComponentState(
            FireRateSelector fireRateSelector,
            (int count, int max)? magazine)
        {
            FireRateSelector = fireRateSelector;
            Magazine = magazine;
        }
    }
}
