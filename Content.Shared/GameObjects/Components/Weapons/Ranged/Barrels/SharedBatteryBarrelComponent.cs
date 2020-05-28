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
    
    [Serializable, NetSerializable]
    public class BatteryBarrelComponentState : ComponentState
    {
        public FireRateSelector FireRateSelector { get; }
        public (int count, int max)? Battery { get; }
        public string SoundGunshot { get; }
        
        public BatteryBarrelComponentState(
            FireRateSelector fireRateSelector, 
            (int count, int max)? battery,
            string soundGunshot) : 
            base(ContentNetIDs.BATTERY_BARREL)
        {
            FireRateSelector = fireRateSelector;
            Battery = battery;
            SoundGunshot = soundGunshot;
        }
    }
}