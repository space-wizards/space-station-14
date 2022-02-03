using System;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Components
{
    [NetworkedComponent()]
    public abstract class SharedRangedWeaponComponent : Component
    {
        // Each RangedWeapon should have a RangedWeapon component +
        // some kind of RangedBarrelComponent (this dictates what ammo is retrieved).
    }

    [Serializable, NetSerializable]
    public sealed class RangedWeaponComponentState : ComponentState
    {
        public FireRateSelector FireRateSelector { get; }

        public RangedWeaponComponentState(
            FireRateSelector fireRateSelector
            )
        {
            FireRateSelector = fireRateSelector;
        }
    }

    /// <summary>
    /// An event raised when the weapon is fired at a position on the map by a client.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class FirePosEvent : EntityEventArgs
    {
        public EntityCoordinates Coordinates;

        public FirePosEvent(EntityCoordinates coordinates)
        {
            Coordinates = coordinates;
        }
    }
}
