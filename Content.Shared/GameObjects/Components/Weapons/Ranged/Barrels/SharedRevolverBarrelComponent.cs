#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels
{
    [Serializable, NetSerializable]
    public class RevolverBarrelComponentState : ComponentState
    {
        public int CurrentSlot { get; }
        public FireRateSelector FireRateSelector { get; }
        public bool?[] Bullets { get; }
        public string SoundGunshot { get; }

        public RevolverBarrelComponentState(
            int currentSlot,
            FireRateSelector fireRateSelector,
            bool?[] bullets,
            string soundGunshot) :
            base(ContentNetIDs.REVOLVER_BARREL)
        {
            CurrentSlot = currentSlot;
            FireRateSelector = fireRateSelector;
            Bullets = bullets;
            SoundGunshot = soundGunshot;
        }
    }
}
