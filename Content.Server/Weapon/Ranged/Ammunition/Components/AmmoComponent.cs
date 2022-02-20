using System;
using Content.Shared.Examine;
using Content.Shared.Sound;
using Content.Shared.Weapons.Ranged.Barrels.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Weapon.Ranged.Ammunition.Components
{
    /// <summary>
    /// Allows this entity to be loaded into a ranged weapon (if the caliber matches)
    /// Generally used for bullets but can be used for other things like bananas
    /// </summary>
    [RegisterComponent]
    [Friend(typeof(GunSystem))]
    public sealed class AmmoComponent : Component, ISerializationHooks
    {
        [DataField("caliber")]
        public BallisticCaliber Caliber { get; } = BallisticCaliber.Unspecified;

        public bool Spent
        {
            get
            {
                if (AmmoIsProjectile)
                {
                    return false;
                }

                return _spent;
            }
            set => _spent = value;
        }

        private bool _spent;

        // TODO: Make it so null projectile = dis
        /// <summary>
        /// Used for anything without a case that fires itself
        /// </summary>
        [DataField("isProjectile")] public bool AmmoIsProjectile;

        /// <summary>
        /// Used for something that is deleted when the projectile is retrieved
        /// </summary>
        [DataField("caseless")]
        public bool Caseless { get; }

        // Rather than managing bullet / case state seemed easier to just have 2 toggles
        // ammoIsProjectile being for a beanbag for example and caseless being for ClRifle rounds

        /// <summary>
        /// For shotguns where they might shoot multiple entities
        /// </summary>
        [DataField("projectilesFired")]
        public int ProjectilesFired { get; } = 1;

        [DataField("projectile", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? ProjectileId;

        // How far apart each entity is if multiple are shot
        [DataField("ammoSpread")]
        public float EvenSpreadAngle { get; } = default;

        /// <summary>
        /// How fast the shot entities travel
        /// </summary>
        [DataField("ammoVelocity")]
        public float Velocity { get; } = 20f;

        [DataField("muzzleFlash")]
        public ResourcePath? MuzzleFlashSprite = new("Objects/Weapons/Guns/Projectiles/bullet_muzzle.png");

        [DataField("soundCollectionEject")]
        public SoundSpecifier SoundCollectionEject { get; } = new SoundCollectionSpecifier("CasingEject");

        void ISerializationHooks.AfterDeserialization()
        {
            // Being both caseless and shooting yourself doesn't make sense
            DebugTools.Assert(!(AmmoIsProjectile && Caseless));

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

    public enum BallisticCaliber
    {
        Unspecified = 0,
        A357, // Placeholder?
        ClRifle,
        SRifle,
        Pistol,
        A35, // Placeholder?
        LRifle,
        Magnum,
        AntiMaterial,
        Shotgun,
        Cap,
        Rocket,
        Dart, // Placeholder
        Grenade,
        Energy,
    }
}
