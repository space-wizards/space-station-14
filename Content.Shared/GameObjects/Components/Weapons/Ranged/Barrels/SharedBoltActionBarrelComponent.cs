#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels
{
    [Serializable, NetSerializable]
    public class BoltActionBarrelComponentState : ComponentState
    {
        public (bool chambered, bool spent) Chamber { get; }
        public FireRateSelector FireRateSelector { get; }
        public (int count, int max)? Magazine { get; }
        public string? SoundGunshot { get; }

        public BoltActionBarrelComponentState(
            (bool chambered, bool spent) chamber,
            FireRateSelector fireRateSelector,
            (int count, int max)? magazine,
            string? soundGunshot) :
            base(ContentNetIDs.BOLTACTION_BARREL)
        {
            Chamber = chamber;
            FireRateSelector = fireRateSelector;
            Magazine = magazine;
            SoundGunshot = soundGunshot;
        }
    }
}
