using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged
{
    public abstract class SharedRangedWeaponComponent : Component
    {
        // Each RangedWeapon should have a RangedWeapon component +
        // some kind of RangedBarrelComponent (this dictates what ammo is retrieved).
        public override string Name => "RangedWeapon";
        public override uint? NetID => ContentNetIDs.RANGED_WEAPON;
    }

    [Serializable, NetSerializable]
    public sealed class RangedWeaponComponentState : ComponentState
    {
        public FireRateSelector FireRateSelector { get; }
        
        public RangedWeaponComponentState(
            FireRateSelector fireRateSelector
            ) : base(ContentNetIDs.RANGED_WEAPON)
        {
            FireRateSelector = fireRateSelector;
        }
    }

    [Serializable, NetSerializable]
    public sealed class FirePosComponentMessage : ComponentMessage
    {
        public GridCoordinates Target { get; }

        public FirePosComponentMessage(GridCoordinates target)
        {
            Target = target;
        }
    }
}
