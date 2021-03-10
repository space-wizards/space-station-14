#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels
{
    [Serializable, NetSerializable]
    public enum AmmoVisuals
    {
        AmmoCount,
        AmmoMax,
        Spent,
    }

    [Serializable, NetSerializable]
    public enum MagazineBarrelVisuals
    {
        MagLoaded
    }

    [Serializable, NetSerializable]
    public enum BarrelBoltVisuals
    {
        BoltOpen,
    }

    [Serializable, NetSerializable]
    public class MagazineBarrelComponentState : ComponentState
    {
        public bool Chambered { get; }
        public FireRateSelector FireRateSelector { get; }
        public (int count, int max)? Magazine { get; }
        public string SoundGunshot { get; }

        public MagazineBarrelComponentState(
            bool chambered,
            FireRateSelector fireRateSelector,
            (int count, int max)? magazine,
            string soundGunshot) :
            base(ContentNetIDs.MAGAZINE_BARREL)
        {
            Chambered = chambered;
            FireRateSelector = fireRateSelector;
            Magazine = magazine;
            SoundGunshot = soundGunshot;
        }
    }
}
