using System;
using Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition
{
    /// <summary>
    /// Allows this entity to be loaded into a ranged weapon (if the caliber matches)
    /// Generally used for bullets but can be used for other things like bananas
    /// </summary>
    [RegisterComponent]
    public class AmmoComponent : Component, IExamine
    {
        public override string Name => "Ammo";
        public BallisticCaliber Caliber => _caliber;
        private BallisticCaliber _caliber;
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
        private bool _ammoIsProjectile;

        /// <summary>
        /// Used for something that is deleted when the projectile is retrieved
        /// </summary>
        public bool Caseless => _caseless;
        private bool _caseless;
        // Rather than managing bullet / case state seemed easier to just have 2 toggles
        // ammoIsProjectile being for a beanbag for example and caseless being for ClRifle rounds

        /// <summary>
        /// For shotguns where they might shoot multiple entities
        /// </summary>
        public int ProjectilesFired => _projectilesFired;
        private int _projectilesFired;
        private string _projectileId;
        // How far apart each entity is if multiple are shot
        public float EvenSpreadAngle => _evenSpreadAngle;
        private float _evenSpreadAngle;
        /// <summary>
        /// How fast the shot entities travel
        /// </summary>
        public float Velocity => _velocity;
        private float _velocity;

        private string _muzzleFlashSprite;

        public string SoundCollectionEject => _soundCollectionEject;
        private string _soundCollectionEject;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            // For shotty of whatever as well
            serializer.DataField(ref _projectileId, "projectile", null);
            serializer.DataField(ref _caliber, "caliber", BallisticCaliber.Unspecified);
            serializer.DataField(ref _projectilesFired, "projectilesFired", 1);
            // Used for shotty to determine overall pellet spread
            serializer.DataField(ref _evenSpreadAngle, "ammoSpread", 0);
            serializer.DataField(ref _velocity, "ammoVelocity", 20.0f);
            serializer.DataField(ref _ammoIsProjectile, "isProjectile", false);
            serializer.DataField(ref _caseless, "caseless", false);
            // Being both caseless and shooting yourself doesn't make sense
            DebugTools.Assert(!(_ammoIsProjectile && _caseless));
            serializer.DataField(ref _muzzleFlashSprite, "muzzleFlash", "Objects/Weapons/Guns/Projectiles/bullet_muzzle.png");
            serializer.DataField(ref _soundCollectionEject, "soundCollectionEject", "CasingEject");

            if (_projectilesFired < 1)
            {
                Logger.Error("Ammo can't have less than 1 projectile");
            }

            if (_evenSpreadAngle > 0 && _projectilesFired == 1)
            {
                Logger.Error("Can't have an even spread if only 1 projectile is fired");
                throw new InvalidOperationException();
            }
        }

        public IEntity TakeBullet(GridCoordinates spawnAtGrid, MapCoordinates spawnAtMap)
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
            if (Owner.TryGetComponent(out AppearanceComponent appearanceComponent))
            {
                appearanceComponent.SetData(AmmoVisuals.Spent, true);
            }

            var entity = spawnAtGrid.GridID != GridId.Invalid ? Owner.EntityManager.SpawnEntity(_projectileId, spawnAtGrid) : Owner.EntityManager.SpawnEntity(_projectileId, spawnAtMap);

            DebugTools.AssertNotNull(entity);
            return entity;
        }

        public void MuzzleFlash(IEntity entity, Angle angle)
        {
            if (_muzzleFlashSprite == null)
            {
                return;
            }

            var time = IoCManager.Resolve<IGameTiming>().CurTime;
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
    }
}
