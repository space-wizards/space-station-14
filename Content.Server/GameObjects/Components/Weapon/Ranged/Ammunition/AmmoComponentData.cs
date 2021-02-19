using System;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition
{
    public partial class AmmoComponentData : ISerializationHooks
    {
        [DataField("projectilesFired")]
        public int? ProjectilesFired;

        [DataField("ammoSpread")]
        public float? EvenSpreadAngle;

        [DataField("isProjectile")]
        public bool? AmmoIsProjectile;

        [DataField("caseless")]
        private bool? Caseless;

        public void AfterDeserialization()
        {
            // Being both caseless and shooting yourself doesn't make sense
            DebugTools.Assert(!(AmmoIsProjectile == true && Caseless == true));

            if (ProjectilesFired < 1)
            {
                Logger.Error("Ammo can't have less than 1 projectile");
            }

            if (EvenSpreadAngle > 0 && ProjectilesFired == 1)
            {
                Logger.Error("Can't have an even spread if only 1 projectile is fired");
                throw new InvalidOperationException();
            }
        }
    }
}
