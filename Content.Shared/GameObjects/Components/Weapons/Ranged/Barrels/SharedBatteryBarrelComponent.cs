using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels
{
    [Serializable, NetSerializable]
    public class BatteryBarrelComponentState : ComponentState
    {
        public FireRateSelector FireRateSelector { get; }
        public (int count, int max)? Magazine { get; }

        public BatteryBarrelComponentState(
            FireRateSelector fireRateSelector,
            (int count, int max)? magazine) :
            base(ContentNetIDs.BATTERY_BARREL)
        {
            FireRateSelector = fireRateSelector;
            Magazine = magazine;
        }
    }
}
