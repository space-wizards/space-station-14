using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Barrels.Components
{
    [Serializable, NetSerializable]
    public sealed class BatteryBarrelComponentState : ComponentState
    {
        public FireRateSelector FireRateSelector { get; }

        public BatteryBarrelComponentState(FireRateSelector fireRateSelector)
        {
            FireRateSelector = fireRateSelector;
        }
    }
}
