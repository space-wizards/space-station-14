using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Projectiles.Components;
using Content.Server.Weapon.Ranged.Ammunition.Components;
using Content.Shared.Camera;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Sound;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Weapon.Ranged.Barrels.Components
{
    /// <summary>
    /// All of the ranged weapon components inherit from this to share mechanics like shooting etc.
    /// Only difference between them is how they retrieve a projectile to shoot (battery, magazine, etc.)
    /// </summary>
#pragma warning disable 618
    public abstract class ServerRangedBarrelComponent : SharedRangedBarrelComponent, IExamine, ISerializationHooks
#pragma warning restore 618
    {
        // There's still some of py01 and PJB's work left over, especially in underlying shooting logic,
        // it's just when I re-organised it changed me as the contributor
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] protected readonly IEntityManager Entities = default!;

        public override FireRateSelector FireRateSelector => _fireRateSelector;

        [DataField("currentSelector")]
        private FireRateSelector _fireRateSelector = FireRateSelector.Safety;

        public override FireRateSelector AllRateSelectors => _fireRateSelector;

        [DataField("fireRate")]
        public override float FireRate { get; } = 2f;

        // _lastFire is when we actually fired (so if we hold the button then recoil doesn't build up if we're not firing)
        private TimeSpan _lastFire;

        public abstract EntityUid? PeekAmmo();
        public abstract EntityUid? TakeProjectile(EntityCoordinates spawnAt);

        // Recoil / spray control
        [DataField("minAngle")]
        private float _minAngleDegrees;

        public Angle MinAngle { get; private set; }

        [DataField("maxAngle")]
        private float _maxAngleDegrees = 45;

        public Angle MaxAngle { get; private set; }

        private Angle _currentAngle = Angle.Zero;

        [DataField("angleDecay")]
        private float _angleDecayDegrees = 20;

        /// <summary>
        /// How slowly the angle's theta decays per second in radians
        /// </summary>
        public float AngleDecay { get; private set; }

        [DataField("angleIncrease")]
        private float? _angleIncreaseDegrees;

        /// <summary>
        /// How quickly the angle's theta builds for every shot fired in radians
        /// </summary>
        public float AngleIncrease { get; private set; }

        // Multiplies the ammo spread to get the final spread of each pellet
        [DataField("ammoSpreadRatio")]
        public float SpreadRatio { get; private set; }

        [DataField("canMuzzleFlash")]
        public bool CanMuzzleFlash { get; } = true;

        // Sounds
        [DataField("soundGunshot", required: true)]
        public SoundSpecifier SoundGunshot { get; set; } = default!;

        [DataField("soundEmpty")]
        public SoundSpecifier SoundEmpty { get; } = new SoundPathSpecifier("/Audio/Weapons/Guns/Empty/empty.ogg");

        void ISerializationHooks.BeforeSerialization()
        {
            _minAngleDegrees = (float) (MinAngle.Degrees * 2);
            _maxAngleDegrees = (float) (MaxAngle.Degrees * 2);
            _angleIncreaseDegrees = MathF.Round(AngleIncrease / ((float) Math.PI / 180f), 2);
            AngleDecay = MathF.Round(AngleDecay / ((float) Math.PI / 180f), 2);
        }

        void ISerializationHooks.AfterDeserialization()
        {
            // This hard-to-read area's dealing with recoil
            // Use degrees in yaml as it's easier to read compared to "0.0125f"
            MinAngle = Angle.FromDegrees(_minAngleDegrees / 2f);

            // Random doubles it as it's +/- so uhh we'll just half it here for readability
            MaxAngle = Angle.FromDegrees(_maxAngleDegrees / 2f);

            _angleIncreaseDegrees ??= 40 / FireRate;
            AngleIncrease = _angleIncreaseDegrees.Value * (float) Math.PI / 180f;

            AngleDecay = _angleDecayDegrees * (float) Math.PI / 180f;

            // For simplicity we'll enforce it this way; ammo determines max spread
            if (SpreadRatio > 1.0f)
            {
                Logger.Error("SpreadRatio must be <= 1.0f for guns");
                throw new InvalidOperationException();
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn(out ServerRangedWeaponComponent rangedWeaponComponent);

            rangedWeaponComponent.Barrel ??= this;
            rangedWeaponComponent.FireHandler += Fire;
            rangedWeaponComponent.WeaponCanFireHandler += WeaponCanFire;
        }

        protected override void OnRemove()
        {
            base.OnRemove();
            if (Entities.TryGetComponent(Owner, out ServerRangedWeaponComponent? rangedWeaponComponent))
            {
                rangedWeaponComponent.Barrel = null;
                rangedWeaponComponent.FireHandler -= Fire;
                rangedWeaponComponent.WeaponCanFireHandler -= WeaponCanFire;
            }
        }

        private Angle GetRecoilAngle(Angle direction)
        {
            var currentTime = _gameTiming.CurTime;
            var timeSinceLastFire = (currentTime - _lastFire).TotalSeconds;
            var newTheta = MathHelper.Clamp(_currentAngle.Theta + AngleIncrease - AngleDecay * timeSinceLastFire, MinAngle.Theta, MaxAngle.Theta);
            _currentAngle = new Angle(newTheta);

            var random = (_robustRandom.NextDouble() - 0.5) * 2;
            var angle = Angle.FromDegrees(direction.Degrees + _currentAngle.Degrees * random);
            return angle;
        }

        public void ChangeFireSelector(FireRateSelector rateSelector)
        {
            if ((rateSelector & AllRateSelectors) != 0)
            {
                _fireRateSelector = rateSelector;
                return;
            }

            throw new InvalidOperationException();
        }

        protected virtual bool WeaponCanFire()
        {
            // If the ServerRangedWeaponComponent gets re-done probably need to add the checks here
            return true;
        }

        /// <summary>
        /// Fires a round of ammo out of the weapon.
        /// </summary>
        /// <param name="shooter">Entity that is operating the weapon, usually the player.</param>
        /// <param name="targetPos">Target position on the map to shoot at.</param>
        private void Fire(EntityUid shooter, Vector2 targetPos)
        {
            if (ShotsLeft == 0)
            {
                SoundSystem.Play(Filter.Broadcast(), SoundEmpty.GetSound(), Owner);
                return;
            }

            var ammo = PeekAmmo();
            if (TakeProjectile(Entities.GetComponent<TransformComponent>(shooter).Coordinates) is not {Valid: true} projectile)
            {
                SoundSystem.Play(Filter.Broadcast(), SoundEmpty.GetSound(), Owner);
                return;
            }

            // At this point firing is confirmed
            var direction = (targetPos - Entities.GetComponent<TransformComponent>(shooter).WorldPosition).ToAngle();
            var angle = GetRecoilAngle(direction);
            // This should really be client-side but for now we'll just leave it here
            if (Entities.HasComponent<CameraRecoilComponent>(shooter))
            {
                var kick = -angle.ToVec() * 0.15f;
                EntitySystem.Get<CameraRecoilSystem>().KickCamera(shooter, kick);
            }

            // This section probably needs tweaking so there can be caseless hitscan etc.
            if (Entities.TryGetComponent(projectile, out HitscanComponent? hitscan))
            {
                FireHitscan(shooter, hitscan, angle);
            }
            else if (Entities.HasComponent<ProjectileComponent>(projectile) &&
                     Entities.TryGetComponent(ammo, out AmmoComponent? ammoComponent))
            {
                FireProjectiles(shooter, projectile, ammoComponent.ProjectilesFired, ammoComponent.EvenSpreadAngle, angle, ammoComponent.Velocity, ammo.Value);

                if (CanMuzzleFlash)
                {
                    EntitySystem.Get<GunSystem>().MuzzleFlash(Owner, ammoComponent, angle);
                }

                if (ammoComponent.Caseless)
                {
                    Entities.DeleteEntity(ammo.Value);
                }
            }
            else
            {
                // Invalid types
                throw new InvalidOperationException();
            }

            SoundSystem.Play(Filter.Broadcast(), SoundGunshot.GetSound(), Owner);

            _lastFire = _gameTiming.CurTime;
        }

        /// <summary>
        /// Drops a single cartridge / shell
        /// Made as a static function just because multiple places need it
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="playSound"></param>
        /// <param name="robustRandom"></param>
        /// <param name="prototypeManager"></param>
        /// <param name="ejectDirections"></param>
        public static void EjectCasing(
            EntityUid entity,
            bool playSound = true,
            Direction[]? ejectDirections = null,
            IRobustRandom? robustRandom = null,
            IPrototypeManager? prototypeManager = null,
            IEntityManager? entities = null)
        {
            IoCManager.Resolve(ref robustRandom, ref prototypeManager, ref entities);

            ejectDirections ??= new[]
                {Direction.East, Direction.North, Direction.NorthWest, Direction.South, Direction.SouthEast, Direction.West};

            const float ejectOffset = 1.8f;
            var ammo = entities.GetComponent<AmmoComponent>(entity);
            var offsetPos = ((robustRandom.NextFloat() - 0.5f) * ejectOffset, (robustRandom.NextFloat() - 0.5f) * ejectOffset);
            entities.GetComponent<TransformComponent>(entity).Coordinates = entities.GetComponent<TransformComponent>(entity).Coordinates.Offset(offsetPos);
            entities.GetComponent<TransformComponent>(entity).LocalRotation = robustRandom.Pick(ejectDirections).ToAngle();

            var coordinates = entities.GetComponent<TransformComponent>(entity).Coordinates;
            SoundSystem.Play(Filter.Broadcast(), ammo.SoundCollectionEject.GetSound(), coordinates, AudioParams.Default.WithVolume(-1));
        }

        /// <summary>
        /// Drops multiple cartridges / shells on the floor
        /// Wraps EjectCasing to make it less toxic for bulk ejections
        /// </summary>
        /// <param name="entities"></param>
        public static void EjectCasings(IEnumerable<EntityUid> entities)
        {
            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var ejectDirections = new[] {Direction.East, Direction.North, Direction.NorthWest, Direction.South, Direction.SouthEast, Direction.West};
            var soundPlayCount = 0;
            var playSound = true;

            foreach (var entity in entities)
            {
                EjectCasing(entity, playSound, ejectDirections, robustRandom, prototypeManager);
                soundPlayCount++;
                if (soundPlayCount > 3)
                {
                    playSound = false;
                }
            }
        }

        #region Firing
        /// <summary>
        /// Handles firing one or many projectiles
        /// </summary>
        private void FireProjectiles(EntityUid shooter, EntityUid baseProjectile, int count, float evenSpreadAngle, Angle angle, float velocity, EntityUid ammo)
        {
            List<Angle>? sprayAngleChange = null;
            if (count > 1)
            {
                evenSpreadAngle *= SpreadRatio;
                sprayAngleChange = Linspace(-evenSpreadAngle / 2, evenSpreadAngle / 2, count);
            }

            var firedProjectiles = new EntityUid[count];
            for (var i = 0; i < count; i++)
            {
                EntityUid projectile;

                if (i == 0)
                {
                    projectile = baseProjectile;
                }
                else
                {
                    projectile = Entities.SpawnEntity(
                        Entities.GetComponent<MetaDataComponent>(baseProjectile).EntityPrototype?.ID,
                        Entities.GetComponent<TransformComponent>(baseProjectile).Coordinates);
                }

                firedProjectiles[i] = projectile;

                Angle projectileAngle;

                if (sprayAngleChange != null)
                {
                    projectileAngle = angle + sprayAngleChange[i];
                }
                else
                {
                    projectileAngle = angle;
                }

                var physics = Entities.GetComponent<IPhysBody>(projectile);
                physics.BodyStatus = BodyStatus.InAir;

                var projectileComponent = Entities.GetComponent<ProjectileComponent>(projectile);
                projectileComponent.IgnoreEntity(shooter);

                // FIXME: Work around issue where inserting and removing an entity from a container,
                // then setting its linear velocity in the same tick resets velocity back to zero.
                // See SharedBroadphaseSystem.HandleContainerInsert()... It sets Awake to false, which causes this.
                projectile.SpawnTimer(TimeSpan.FromMilliseconds(25), () =>
                {
                    Entities.GetComponent<IPhysBody>(projectile)
                        .LinearVelocity = projectileAngle.ToVec() * velocity;
                });


                Entities.GetComponent<TransformComponent>(projectile).WorldRotation = projectileAngle + MathHelper.PiOver2;
            }

            Entities.EventBus.RaiseLocalEvent(Owner, new GunShotEvent(firedProjectiles));
            Entities.EventBus.RaiseLocalEvent(ammo, new AmmoShotEvent(firedProjectiles));
        }

        /// <summary>
        ///     Returns a list of numbers that form a set of equal intervals between the start and end value. Used to calculate shotgun spread angles.
        /// </summary>
        private List<Angle> Linspace(double start, double end, int intervals)
        {
            DebugTools.Assert(intervals > 1);

            var linspace = new List<Angle>(intervals);

            for (var i = 0; i <= intervals - 1; i++)
            {
                linspace.Add(Angle.FromDegrees(start + (end - start) * i / (intervals - 1)));
            }
            return linspace;
        }

        /// <summary>
        /// Fires hitscan entities and then displays their effects
        /// </summary>
        private void FireHitscan(EntityUid shooter, HitscanComponent hitscan, Angle angle)
        {
            var ray = new CollisionRay(Entities.GetComponent<TransformComponent>(Owner).Coordinates.ToMapPos(Entities), angle.ToVec(), (int) hitscan.CollisionMask);
            var physicsManager = EntitySystem.Get<SharedPhysicsSystem>();
            var rayCastResults = physicsManager.IntersectRay(Entities.GetComponent<TransformComponent>(Owner).MapID, ray, hitscan.MaxLength, shooter, false).ToList();

            if (rayCastResults.Count >= 1)
            {
                var result = rayCastResults[0];
                var distance = result.Distance;
                hitscan.FireEffects(shooter, distance, angle, result.HitEntity);
                var dmg = EntitySystem.Get<DamageableSystem>().TryChangeDamage(result.HitEntity, hitscan.Damage);
                if (dmg != null)
                    EntitySystem.Get<AdminLogSystem>().Add(LogType.HitScanHit,
                        $"{Entities.ToPrettyString(shooter):user} hit {Entities.ToPrettyString(result.HitEntity):target} using {Entities.ToPrettyString(hitscan.Owner):used} and dealt {dmg.Total:damage} damage");
            }
            else
            {
                hitscan.FireEffects(shooter, hitscan.MaxLength, angle);
            }
        }
        #endregion

        public virtual void Examine(FormattedMessage message, bool inDetailsRange)
        {
            var fireRateMessage = Loc.GetString(FireRateSelector switch
            {
                FireRateSelector.Safety => "server-ranged-barrel-component-on-examine-fire-rate-safety-description",
                FireRateSelector.Single => "server-ranged-barrel-component-on-examine-fire-rate-single-description",
                FireRateSelector.Automatic => "server-ranged-barrel-component-on-examine-fire-rate-automatic-description",
                _ => throw new IndexOutOfRangeException()
            });

            message.AddText(fireRateMessage);
        }
    }

    /// <summary>
    /// Raised on a gun when it fires projectiles.
    /// </summary>
    public sealed class GunShotEvent : EntityEventArgs
    {
        /// <summary>
        /// Uid of the entity that shot.
        /// </summary>
        public EntityUid Uid;

        public readonly EntityUid[] FiredProjectiles;

        public GunShotEvent(EntityUid[] firedProjectiles)
        {
            FiredProjectiles = firedProjectiles;
        }
    }

    /// <summary>
    /// Raised on ammo when it is fired.
    /// </summary>
    public sealed class AmmoShotEvent : EntityEventArgs
    {
        /// <summary>
        /// Uid of the entity that shot.
        /// </summary>
        public EntityUid Uid;

        public readonly EntityUid[] FiredProjectiles;

        public AmmoShotEvent(EntityUid[] firedProjectiles)
        {
            FiredProjectiles = firedProjectiles;
        }
    }
}
