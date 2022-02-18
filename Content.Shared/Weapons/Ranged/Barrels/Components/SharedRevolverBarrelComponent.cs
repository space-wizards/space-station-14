using System;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Barrels.Components
{
    [Serializable, NetSerializable]
    public sealed class RevolverBarrelComponentState : ComponentState
    {
        public int CurrentSlot { get; }
        public FireRateSelector FireRateSelector { get; }
        public bool?[] Bullets { get; }
        public string? SoundGunshot { get; }

        public RevolverBarrelComponentState(
            int currentSlot,
            FireRateSelector fireRateSelector,
            bool?[] bullets,
            string? soundGunshot)
        {
            CurrentSlot = currentSlot;
            FireRateSelector = fireRateSelector;
            Bullets = bullets;
            SoundGunshot = soundGunshot;
        }
    }
}
