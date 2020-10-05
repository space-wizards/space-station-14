#nullable enable
using System;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged
{
    /// <summary>
    ///     Allows this entity to be loaded into a ranged weapon (if the caliber matches)
    ///     Generally used for bullets but can be used for other things like bananas
    /// </summary>
    public abstract class SharedAmmoComponent : Component
    {
        public override string Name => "Ammo";

        public override uint? NetID => ContentNetIDs.AMMO;

        [ViewVariables]
        public BallisticCaliber Caliber { get; private set; }

        public virtual bool Spent { get; set; }

        public bool AmmoIsProjectile => _ammoIsProjectile;
        
        /// <summary>
        ///     Used for anything without a case that fires itself, like if you loaded a banana into a banana launcher.
        /// </summary>
        private bool _ammoIsProjectile;

        /// <summary>
        ///     Used for ammo that is deleted when the projectile is retrieved
        /// </summary>
        [ViewVariables]
        public bool Caseless { get; private set; }
        
        // Rather than managing bullet / case state seemed easier to just have 2 toggles
        // ammoIsProjectile being for a beanbag for example and caseless being for ClRifle rounds

        /// <summary>
        ///     For shotguns where they might shoot multiple entities
        /// </summary>
        [ViewVariables]
        public byte ProjectilesFired { get; private set; }

        /// <summary>
        ///     Prototype ID of the entity to be spawned (projectile or hitscan).
        /// </summary>
        [ViewVariables]
        public string ProjectileId { get; private set; } = default!;

        public bool IsHitscan => IoCManager.Resolve<IPrototypeManager>().Index<HitscanPrototype>(ProjectileId) != null;
        
        /// <summary>
        ///     How far apart each entity is if multiple are shot, like with a shotgun.
        /// </summary>
        [ViewVariables]
        public float EvenSpreadAngle { get; private set; }
        
        /// <summary>
        ///     How fast the shot entities travel
        /// </summary>
        [ViewVariables]
        public float Velocity { get; private set; }

        public string? SoundCollectionEject { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            // For shotty or whatever as well
            serializer.DataReadWriteFunction(
                "projectile", 
                string.Empty, 
                projectile => ProjectileId = projectile,
                () => ProjectileId);
            
            serializer.DataReadWriteFunction(
                "caliber", 
                BallisticCaliber.Unspecified, 
                caliber => Caliber = caliber,
                () => Caliber);

            serializer.DataReadWriteFunction(
                "projectilesFired", 
                1, 
                numFired => ProjectilesFired = (byte) numFired,
                () => ProjectilesFired);

            serializer.DataReadWriteFunction(
                "ammoSpread", 
                0, 
                spread => EvenSpreadAngle = spread,
                () => EvenSpreadAngle);
            
            serializer.DataReadWriteFunction(
                "ammoVelocity", 
                20.0f, 
                velocity => Velocity = velocity,
                () => Velocity);
            
            serializer.DataField(ref _ammoIsProjectile, "isProjectile", false);
            
            serializer.DataReadWriteFunction(
                "caseless", 
                false, 
                caseless => Caseless = caseless,
                () => Caseless);
            
            // Being both caseless and shooting yourself doesn't make sense
            DebugTools.Assert(!(_ammoIsProjectile && Caseless));

            serializer.DataReadWriteFunction(
                "soundCollectionEject", 
                "CasingEject", 
                soundEject => SoundCollectionEject = soundEject,
                () => SoundCollectionEject);

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

        public bool CanFire()
        {
            if (Spent && !_ammoIsProjectile)
            {
                return false;
            }

            return true;
        }
    }

    [Serializable, NetSerializable]
    public sealed class AmmoComponentState : ComponentState
    {
        public bool Spent { get; }

        public AmmoComponentState(bool spent) : base(ContentNetIDs.AMMO)
        {
            Spent = spent;
        }
    }
}