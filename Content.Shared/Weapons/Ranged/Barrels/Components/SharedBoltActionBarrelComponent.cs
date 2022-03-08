using System;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Barrels.Components
{
    [Serializable, NetSerializable]
    public sealed class BoltActionBarrelComponentState : ComponentState
    {
        public (bool chambered, bool spent) Chamber { get; }
        public FireRateSelector FireRateSelector { get; }
        public (int count, int max)? Magazine { get; }
        public string? SoundGunshot { get; }

        public BoltActionBarrelComponentState(
            (bool chambered, bool spent) chamber,
            FireRateSelector fireRateSelector,
            (int count, int max)? magazine,
            string? soundGunshot)
        {
            Chamber = chamber;
            FireRateSelector = fireRateSelector;
            Magazine = magazine;
            SoundGunshot = soundGunshot;
        }
    }
}
