using System;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition
{
    /// <summary>
    /// Allows this entity to be loaded into a ranged weapon (if the caliber matches)
    /// Generally used for bullets but can be used for other things like bananas
    /// </summary>
    [RegisterComponent]
    public class AmmoComponent : Component, IExamine, ISerializationHooks
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "Ammo";

        [DataField("caliber")]
        public BallisticCaliber Caliber { get; } = BallisticCaliber.Unspecified;

        public bool Spent
        {
            get
            {
                if (_ammoIsProjectile)
                {
                    return false;
                }

                return _spent;
            }
        }

        private bool _spent;

        /// <summary>
        /// Used for anything without a case that fires itself
        /// </summary>
        [DataField("isProjectile")]
        private bool _ammoIsProjectile;

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

        [DataField("projectile")]
        private string? _projectileId;

        // How far apart each entity is if multiple are shot
        [DataField("ammoSpread")]
        public float EvenSpreadAngle { get; } = default;

        /// <summary>
        /// How fast the shot entities travel
        /// </summary>
        [DataField("ammoVelocity")]
        public float Velocity { get; } = 20f;

        [DataField("muzzleFlash")]
        private string _muzzleFlashSprite = "Objects/Weapons/Guns/Projectiles/bullet_muzzle.png";

        [DataField("soundCollectionEject")]
        public string? SoundCollectionEject { get; } = "CasingEject";

        void ISerializationHooks.AfterDeserialization()
        {
            // Being both caseless and shooting yourself doesn't make sense
            DebugTools.Assert(!(_ammoIsProjectile == true && Caseless == true));

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

        public IEntity? TakeBullet(EntityCoordinates spawnAt)
        {
            if (_ammoIsProjectile)
            {
                return Owner;
            }

            if (_spent)
            {
                return null;
            }

            _spent = true;
            if (Owner.TryGetComponent(out AppearanceComponent? appearanceComponent))
            {
                appearanceComponent.SetData(AmmoVisuals.Spent, true);
            }

            var entity = Owner.EntityManager.SpawnEntity(_projectileId, spawnAt);

            return entity;
        }

        public void MuzzleFlash(IEntity entity, Angle angle)
        {
            if (_muzzleFlashSprite == null)
            {
                return;
            }

            var time = _gameTiming.CurTime;
            var deathTime = time + TimeSpan.FromMilliseconds(200);
            // Offset the sprite so it actually looks like it's coming from the gun
            var offset = angle.ToVec().Normalized / 2;

            var message = new EffectSystemMessage
            {
                EffectSprite = _muzzleFlashSprite,
                Born = time,
                DeathTime = deathTime,
                AttachedEntityUid = entity.Uid,
                AttachedOffset = offset,
                //Rotated from east facing
                Rotation = (float) angle.Theta,
                Color = Vector4.Multiply(new Vector4(255, 255, 255, 255), 1.0f),
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Shaded = false
            };
            EntitySystem.Get<EffectSystem>().CreateParticle(message);
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            var text = Loc.GetString("It's [color=white]{0}[/color] ammo.", Caliber);
            message.AddMarkup(text);
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
        CreamPie, // I can't wait for this enum to be a prototype type...
    }
}
