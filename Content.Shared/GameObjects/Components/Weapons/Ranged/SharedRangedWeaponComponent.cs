using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged
{
    public class SharedRangedWeaponComponent : Component
    {
        private float _fireRate;
        private bool _automatic;
        public override string Name => "RangedWeapon";
        public override uint? NetID => ContentNetIDs.RANGED_WEAPON;

        /// <summary>
        ///     If true, this weapon is fully automatic, holding down left mouse button will keep firing it.
        /// </summary>
        public bool Automatic => _automatic;

        /// <summary>
        ///     If the weapon is automatic, controls how many shots can be fired per second.
        /// </summary>
        public float FireRate => _fireRate;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _fireRate, "firerate", 4);
            serializer.DataField(ref _automatic, "automatic", false);
        }

        [Serializable, NetSerializable]
        protected class FireMessage : ComponentMessage
        {
            public readonly GridCoordinates Target;
            public readonly int Tick;

            public FireMessage(GridCoordinates target, int tick)
            {
                Target = target;
                Tick = tick;
            }
        }
    }
}
