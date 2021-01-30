using System;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition
{
    public partial class AmmoComponentData
    {
        [CustomYamlField("projectilesFired")]
        public int? ProjectilesFired;

        [CustomYamlField("ammoSpread")]
        public float? EvenSpreadAngle;

        [CustomYamlField("isProjectile")]
        public bool? AmmoIsProjectile;

        [CustomYamlField("caseless")]
        private bool? Caseless;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            // For shotty of whatever as well
            serializer.DataField(ref ProjectilesFired, "projectilesFired", null);
            // Used for shotty to determine overall pellet spread
            serializer.DataField(ref EvenSpreadAngle, "ammoSpread", null);
            serializer.DataField(ref AmmoIsProjectile, "isProjectile", null);
            serializer.DataField(ref Caseless, "caseless", null);
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
