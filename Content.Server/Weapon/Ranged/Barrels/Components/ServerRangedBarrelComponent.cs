using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Barrels
{
    /// <summary>
    /// All of the ranged weapon components inherit from this to share mechanics like shooting etc.
    /// Only difference between them is how they retrieve a projectile to shoot (battery, magazine, etc.)
    /// </summary>
    public abstract class ServerRangedBarrelComponent : SharedRangedBarrelComponent, IUse, IInteractUsing, IExamine, ISerializationHooks
    {
        // There's still some of py01 and PJB's work left over, especially in underlying shooting logic,
        // it's just when I re-organised it changed me as the contributor
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override FireRateSelector FireRateSelector => _fireRateSelector;

        [DataField("currentSelector")]
        private FireRateSelector _fireRateSelector = FireRateSelector.Safety;

        public override FireRateSelector AllRateSelectors => _fireRateSelector;

        [DataField("allSelectors")]
        private FireRateSelector _allRateSelectors;

        [DataField("fireRate")]
        public override float FireRate { get; } = 2f;

        // _lastFire is when we actually fired (so if we hold the button then recoil doesn't build up if we're not firing)
        private TimeSpan _lastFire;

        public abstract IEntity? PeekAmmo();
        public abstract IEntity? TakeProjectile(EntityCoordinates spawnAt);

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
        [DataField("soundGunshot")]
        public string? SoundGunshot { get; set; }

        [DataField("soundEmpty")]
        public string SoundEmpty { get; } = "/Audio/Weapons/Guns/Empty/empty.ogg";

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

        public override void OnAdd()
        {
            base.OnAdd();

            Owner.EnsureComponentWarn(out ServerRangedWeaponComponent rangedWeaponComponent);

            rangedWeaponComponent.Barrel ??= this;
            rangedWeaponComponent.FireHandler += Fire;
            rangedWeaponComponent.WeaponCanFireHandler += WeaponCanFire;
        }

        public override void OnRemove()
        {
            base.OnRemove();
            if (Owner.TryGetComponent(out ServerRangedWeaponComponent? rangedWeaponComponent))
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

        public abstract bool UseEntity(UseEntityEventArgs eventArgs);

        public abstract Task<bool> InteractUsing(InteractUsingEventArgs eventArgs);

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
        private void Fire(IEntity shooter, Vector2 targetPos)
        {
            if (ShotsLeft == 0)
            {
                if (SoundEmpty != null)
                {
                    SoundSystem.Play(Filter.Broadcast(), SoundEmpty, Owner.Transform.Coordinates);
                }
                return;
            }

            var ammo = PeekAmmo();
            var projectile = TakeProjectile(shooter.Transform.Coordinates);
            if (projectile == null)
            {
                SoundSystem.Play(Filter.Broadcast(), SoundEmpty, Owner.Transform.Coordinates);
                return;
            }

            // At this point firing is confirmed
            var direction = (targetPos - shooter.Transform.WorldPosition).ToAngle();
            var angle = GetRecoilAngle(direction);
            // This should really be client-side but for now we'll just leave it here
            if (shooter.TryGetComponent(out CameraRecoilComponent? recoilComponent))
            {
                recoilComponent.Kick(-angle.ToVec() * 0.15f);
            }


            // This section probably needs tweaking so there can be caseless hitscan etc.
            if (projectile.TryGetComponent(out HitscanComponent? hitscan))
            {
                FireHitscan(shooter, hitscan, angle);
            }
            else if (projectile.HasComponent<ProjectileComponent>() &&
                     ammo != null &&
                     ammo.TryGetComponent(out AmmoComponent? ammoComponent))
            {
                FireProjectiles(shooter, projectile, ammoComponent.ProjectilesFired, ammoComponent.EvenSpreadAngle, angle, ammoComponent.Velocity, ammo);

                if (CanMuzzleFlash)
                {
                    ammoComponent.MuzzleFlash(Owner, angle);
                }

                if (ammoComponent.Caseless)
                {
                    ammo.Delete();
                }
            }
            else
            {
                // Invalid types
                throw new InvalidOperationException();
            }

            if (!string.IsNullOrEmpty(SoundGunshot))
            {
                SoundSystem.Play(Filter.Broadcast(), SoundGunshot, Owner.Transform.Coordinates);
            }

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
            IEntity entity,
            bool playSound = true,
            IRobustRandom? robustRandom = null,
            IPrototypeManager? prototypeManager = null,
            Direction[]? ejectDirections = null)
        {
            robustRandom ??= IoCManager.Resolve<IRobustRandom>();
            ejectDirections ??= new[]
                {Direction.East, Direction.North, Direction.NorthWest, Direction.South, Direction.SouthEast, Direction.West};

            const float ejectOffset = 1.8f;
            var ammo = entity.GetComponent<AmmoComponent>();
            var offsetPos = ((robustRandom.NextFloat() - 0.5f) * ejectOffset, (robustRandom.NextFloat() - 0.5f) * ejectOffset);
            entity.Transform.Coordinates = entity.Transform.Coordinates.Offset(offsetPos);
            entity.Transform.LocalRotation = robustRandom.Pick(ejectDirections).ToAngle();

            if (ammo.SoundCollectionEject == null || !playSound)
            {
                return;
            }

            prototypeManager ??= IoCManager.Resolve<IPrototypeManager>();

            var soundCollection = prototypeManager.Index<SoundCollectionPrototype>(ammo.SoundCollectionEject);
            var randomFile = robustRandom.Pick(soundCollection.PickFiles);
            SoundSystem.Play(Filter.Broadcast(), randomFile, entity.Transform.Coordinates, AudioParams.Default.WithVolume(-1));
        }

        /// <summary>
        /// Drops multiple cartridges / shells on the floor
        /// Wraps EjectCasing to make it less toxic for bulk ejections
        /// </summary>
        /// <param name="entities"></param>
        public static void EjectCasings(IEnumerable<IEntity> entities)
        {
            var robustRandom = IoCManager.Resolve<IRobustRandom>();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var ejectDirections = new[] {Direction.East, Direction.North, Direction.NorthWest, Direction.South, Direction.SouthEast, Direction.West};
            var soundPlayCount = 0;
            var playSound = true;

            foreach (var entity in entities)
            {
                EjectCasing(entity, playSound, robustRandom, prototypeManager, ejectDirections);
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
        private void FireProjectiles(IEntity shooter, IEntity baseProjectile, int count, float evenSpreadAngle, Angle angle, float velocity, IEntity ammo)
        {
            List<Angle>? sprayAngleChange = null;
            if (count > 1)
            {
                evenSpreadAngle *= SpreadRatio;
                sprayAngleChange = Linspace(-evenSpreadAngle / 2, evenSpreadAngle / 2, count);
            }

            var firedProjectiles = new List<IEntity>();
            for (var i = 0; i < count; i++)
            {
                IEntity projectile;

                if (i == 0)
                {
                    projectile = baseProjectile;
                }
                else
                {
                    projectile =
                        Owner.EntityManager.SpawnEntity(baseProjectile.Prototype?.ID, baseProjectile.Transform.Coordinates);
                }
                firedProjectiles.Add(projectile);

                Angle projectileAngle;

                if (sprayAngleChange != null)
                {
                    projectileAngle = angle + sprayAngleChange[i];
                }
                else
                {
                    projectileAngle = angle;
                }

                var physics = projectile.GetComponent<IPhysBody>();
                physics.BodyStatus = BodyStatus.InAir;

                var projectileComponent = projectile.GetComponent<ProjectileComponent>();
                projectileComponent.IgnoreEntity(shooter);

                // FIXME: Work around issue where inserting and removing an entity from a container,
                // then setting its linear velocity in the same tick resets velocity back to zero.
                // See SharedBroadPhaseSystem.HandleContainerInsert()... It sets Awake to false, which causes this.
                projectile.SpawnTimer(TimeSpan.FromMilliseconds(25), () =>
                {
                    projectile
                        .GetComponent<IPhysBody>()
                        .LinearVelocity = projectileAngle.ToVec() * velocity;
                });


                projectile.Transform.LocalRotation = projectileAngle + MathHelper.PiOver2;
            }
            ammo.SendMessage(this, new BarrelFiredMessage(firedProjectiles));
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
        private void FireHitscan(IEntity shooter, HitscanComponent hitscan, Angle angle)
        {
            var ray = new CollisionRay(Owner.Transform.Coordinates.ToMapPos(Owner.EntityManager), angle.ToVec(), (int) hitscan.CollisionMask);
            var physicsManager = EntitySystem.Get<SharedBroadPhaseSystem>();
            var rayCastResults = physicsManager.IntersectRay(Owner.Transform.MapID, ray, hitscan.MaxLength, shooter, false).ToList();

            if (rayCastResults.Count >= 1)
            {
                var result = rayCastResults[0];
                var distance = result.Distance;
                hitscan.FireEffects(shooter, distance, angle, result.HitEntity);

                if (!result.HitEntity.TryGetComponent(out IDamageableComponent? damageable))
                    return;

                damageable.ChangeDamage(hitscan.DamageType, (int)Math.Round(hitscan.Damage, MidpointRounding.AwayFromZero), false, Owner);
                //I used Math.Round over Convert.toInt32, as toInt32 always rounds to
                //even numbers if halfway between two numbers, rather than rounding to nearest
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
                FireRateSelector.Safety => "Its safety is enabled.",
                FireRateSelector.Single => "It's in single fire mode.",
                FireRateSelector.Automatic => "It's in automatic fire mode.",
                _ => throw new IndexOutOfRangeException()
            });

            message.AddText(fireRateMessage);
        }
    }

    public class BarrelFiredMessage : ComponentMessage
    {
        public readonly List<IEntity> FiredProjectiles;

        public BarrelFiredMessage(List<IEntity> firedProjectiles)
        {
            FiredProjectiles = firedProjectiles;
        }
    }
}
