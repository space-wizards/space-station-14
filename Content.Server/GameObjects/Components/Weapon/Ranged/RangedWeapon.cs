using SS14.Shared.GameObjects;
using Content.Server.GameObjects.EntitySystems;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Map;

namespace Content.Server.GameObjects.Components.Weapon.Ranged
{
    public class RangedWeaponComponent : Component, IAfterAttack
    {
        public override string Name => "RangedWeapon";

        void IAfterAttack.Afterattack(IEntity user, GridLocalCoordinates clicklocation, IEntity attacked)
        {
            if (UserCanFire(user) && WeaponCanFire())
            {
                Fire(user, clicklocation);
            }
        }

        protected virtual bool WeaponCanFire()
        {
            return true;
        }

        protected virtual bool UserCanFire(IEntity user)
        {
            return true;
        }

        protected virtual void Fire(IEntity user, GridLocalCoordinates clicklocation)
        {
            return;
        }
    }
}
