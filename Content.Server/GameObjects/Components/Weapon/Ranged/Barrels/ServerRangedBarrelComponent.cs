using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Barrels
{
    /// <summary>
    /// All of the ranged weapon components inherit from this to share mechanics like shooting etc.
    /// Only difference between them is how they retrieve a projectile to shoot (battery, magazine, etc.)
    /// </summary>
    public abstract class ServerRangedBarrelComponent : SharedRangedBarrelComponent, IUse, IInteractUsing, IExamine
    {
        // There's still some of py01 and PJB's work left over, especially in underlying shooting logic,
        // it's just when I re-organised it changed me as the contributor
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override FireRateSelector FireRateSelector => _fireRateSelector;
        private FireRateSelector _fireRateSelector;
        public override FireRateSelector AllRateSelectors => _fireRateSelector;
        private FireRateSelector _allRateSelectors;
        public override float FireRate => _fireRate;
        private float _fireRate;

        // _lastFire is when we actually fired (so if we hold the button then recoil doesn't build up if we're not firing)
        private TimeSpan _lastFire;

        public abstract IEntity PeekAmmo();
        public abstract IEntity TakeProjectile(EntityCoordinates spawnAtGrid, MapCoordinates spawnAtMap);

        // Recoil / spray control
        private Angle _minAngle;
        private Angle _maxAngle;
        private Angle _currentAngle = Angle.Zero;
        /// <summary>
        /// How slowly the angle's theta decays per second in radians
        /// </summary>
        private float _angleDecay;
        /// <summary>
        /// How quickly the angle's theta builds for every shot fired in radians
        /// </summary>
        private float _angleIncrease;
        // Multiplies the ammo spread to get the final spread of each pellet
        private float _spreadRatio;

        public bool CanMuzzleFlash => _canMuzzleFlash;
        private bool _canMuzzleFlash = true;

        // Sounds
        public string SoundGunshot
        {
            get => _soundGunshot;
            set => _soundGunshot = value;
        }
        private string _soundGunshot;
        public string SoundEmpty => _soundEmpty;
        private string _soundEmpty;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _fireRateSelector, "currentSelector", FireRateSelector.Safety);
            serializer.DataField(ref _fireRate, "fireRate", 2.0f);

            // This hard-to-read area's dealing with recoil
            // Use degrees in yaml as it's easier to read compared to "0.0125f"
            serializer.DataReadWriteFunction(
                "minAngle",
                0,
                angle => _minAngle = Angle.FromDegrees(angle / 2f),
                () => _minAngle.Degrees * 2);

            // Random doubles it as it's +/- so uhh we'll just half it here for readability
            serializer.DataReadWriteFunction(
                "maxAngle",
                45,
                angle => _maxAngle = Angle.FromDegrees(angle / 2f),
                () => _maxAngle.Degrees * 2);

            serializer.DataReadWriteFunction(
                "angleIncrease",
                40 / _fireRate,
                angle => _angleIncrease = angle * (float) Math.PI / 180f,
                () => MathF.Round(_angleIncrease / ((float) Math.PI / 180f), 2));

            serializer.DataReadWriteFunction(
                "angleDecay",
                20f,
                angle => _angleDecay = angle * (float) Math.PI / 180f,
                () => MathF.Round(_angleDecay / ((float) Math.PI / 180f), 2));

            serializer.DataField(ref _spreadRatio, "ammoSpreadRatio", 1.0f);

            serializer.DataReadWriteFunction(
                "allSelectors",
                new List<FireRateSelector>(),
                selectors => selectors.ForEach(selector => _allRateSelectors |= selector),
                () =>
                {
                    var types = new List<FireRateSelector>();

                    foreach (FireRateSelector selector in Enum.GetValues(typeof(FireRateSelector)))
                    {
                        if ((_allRateSelectors & selector) != 0)
                        {
                            types.Add(selector);
                        }
                    }

                    return types;
                });

            // For simplicity we'll enforce it this way; ammo determines max spread
            if (_spreadRatio > 1.0f)
            {
                Logger.Error("SpreadRatio must be <= 1.0f for guns");
                throw new InvalidOperationException();
            }

            serializer.DataField(ref _canMuzzleFlash, "canMuzzleFlash", true);

            // Sounds
            serializer.DataField(ref _soundGunshot, "soundGunshot", null);
            serializer.DataField(ref _soundEmpty, "soundEmpty", "/Audio/Weapons/Guns/Empty/empty.ogg");
        }

        public override void OnAdd()
        {
            base.OnAdd();

            if (!Owner.EnsureComponent(out ServerRangedWeaponComponent rangedWeaponComponent))
            {
                Logger.Warning(
                    $"Entity {Owner.Name} at {Owner.Transform.MapPosition} didn't have a {nameof(ServerRangedWeaponComponent)}");
            }

            rangedWeaponComponent.Barrel ??= this;
            rangedWeaponComponent.FireHandler += Fire;
            rangedWeaponComponent.WeaponCanFireHandler += WeaponCanFire;
        }

        public override void OnRemove()
        {
            base.OnRemove();
            if (Owner.TryGetComponent(out ServerRangedWeaponComponent rangedWeaponComponent))
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
            var newTheta = MathHelper.Clamp(_currentAngle.Theta + _angleIncrease - _angleDecay * timeSinceLastFire, _minAngle.Theta, _maxAngle.Theta);
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
            var soundSystem = EntitySystem.Get<AudioSystem>();
            if (ShotsLeft == 0)
            {
                if (_soundEmpty != null)
                {
                    soundSystem.PlayAtCoords(_soundEmpty, Owner.Transform.Coordinates);
                }
                return;
            }

            var ammo = PeekAmmo();
            var projectile = TakeProjectile(shooter.Transform.Coordinates, shooter.Transform.MapPosition);
            if (projectile == null)
            {
                soundSystem.PlayAtCoords(_soundEmpty, Owner.Transform.Coordinates);
                return;
            }

            // At this point firing is confirmed
            var direction = (targetPos - shooter.Transform.WorldPosition).ToAngle();
            var angle = GetRecoilAngle(direction);
            // This should really be client-side but for now we'll just leave it here
            if (shooter.TryGetComponent(out CameraRecoilComponent recoilComponent))
            {
                recoilComponent.Kick(-angle.ToVec() * 0.15f);
            }


            // This section probably needs tweaking so there can be caseless hitscan etc.
            if (projectile.TryGetComponent(out HitscanComponent hitscan))
            {
                FireHitscan(shooter, hitscan, angle);
            }
            else if (projectile.HasComponent<ProjectileComponent>())
            {
                var ammoComponent = ammo.GetComponent<AmmoComponent>();

                FireProjectiles(shooter, projectile, ammoComponent.ProjectilesFired, ammoComponent.EvenSpreadAngle, angle, ammoComponent.Velocity);

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

            soundSystem.PlayAtCoords(_soundGunshot, Owner.Transform.Coordinates);
            _lastFire = _gameTiming.CurTime;

            return;
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
            IRobustRandom robustRandom = null,
            IPrototypeManager prototypeManager = null,
            Direction[] ejectDirections = null)
        {
            if (robustRandom == null)
            {
                robustRandom = IoCManager.Resolve<IRobustRandom>();
            }

            if (ejectDirections == null)
            {
                ejectDirections = new[] {Direction.East, Direction.North, Direction.South, Direction.West};
            }

            const float ejectOffset = 0.2f;
            var ammo = entity.GetComponent<AmmoComponent>();
            var offsetPos = (robustRandom.NextFloat() * ejectOffset, robustRandom.NextFloat() * ejectOffset);
            entity.Transform.Coordinates = entity.Transform.Coordinates.Offset(offsetPos);
            entity.Transform.LocalRotation = robustRandom.Pick(ejectDirections).ToAngle();

            if (ammo.SoundCollectionEject == null || !playSound)
            {
                return;
            }

            if (prototypeManager == null)
            {
                prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            }

            var soundCollection = prototypeManager.Index<SoundCollectionPrototype>(ammo.SoundCollectionEject);
            var randomFile = robustRandom.Pick(soundCollection.PickFiles);
            EntitySystem.Get<AudioSystem>().PlayAtCoords(randomFile, entity.Transform.Coordinates, AudioParams.Default.WithVolume(-1));
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
            var ejectDirections = new[] {Direction.East, Direction.North, Direction.South, Direction.West};
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
        private void FireProjectiles(IEntity shooter, IEntity baseProjectile, int count, float evenSpreadAngle, Angle angle, float velocity)
        {
            List<Angle> sprayAngleChange = null;
            if (count > 1)
            {
                evenSpreadAngle *= _spreadRatio;
                sprayAngleChange = Linspace(-evenSpreadAngle / 2, evenSpreadAngle / 2, count);
            }

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
                        Owner.EntityManager.SpawnEntity(baseProjectile.Prototype.ID, Owner.Transform.MapPosition);
                }

                Angle projectileAngle;

                if (sprayAngleChange != null)
                {
                    projectileAngle = angle + sprayAngleChange[i];
                }
                else
                {
                    projectileAngle = angle;
                }

                var collidableComponent = projectile.GetComponent<ICollidableComponent>();
                collidableComponent.Status = BodyStatus.InAir;
                projectile.Transform.WorldPosition = Owner.Transform.MapPosition.Position;

                var projectileComponent = projectile.GetComponent<ProjectileComponent>();
                projectileComponent.IgnoreEntity(shooter);

                projectile
                    .GetComponent<ICollidableComponent>()
                    .EnsureController<BulletController>()
                    .LinearVelocity = projectileAngle.ToVec() * velocity;

                projectile.Transform.LocalRotation = projectileAngle.Theta;
            }
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
            var ray = new CollisionRay(Owner.Transform.Coordinates.Position, angle.ToVec(), (int) hitscan.CollisionMask);
            var physicsManager = IoCManager.Resolve<IPhysicsManager>();
            var rayCastResults = physicsManager.IntersectRay(Owner.Transform.MapID, ray, hitscan.MaxLength, shooter, false).ToList();

            if (rayCastResults.Count >= 1)
            {
                var result = rayCastResults[0];
                var distance = result.HitEntity != null ? result.Distance : hitscan.MaxLength;
                hitscan.FireEffects(shooter, distance, angle, result.HitEntity);

                if (result.HitEntity == null || !result.HitEntity.TryGetComponent(out IDamageableComponent damageable))
                {
                    return;
                }

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
}
