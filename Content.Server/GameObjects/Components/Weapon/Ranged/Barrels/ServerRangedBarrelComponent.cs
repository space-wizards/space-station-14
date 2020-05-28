using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.Components.Weapon.Ranged.Ammunition;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Weapons.Ranged;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Barrels
{
    /// <summary>
    /// All of the ranged weapon components inherit from this to share mechanics like shooting etc.
    /// Only difference between them is how they retrieve a projectile to shoot (battery, magazine, etc.)
    /// </summary>
    public abstract class ServerRangedBarrelComponent : SharedRangedBarrelComponent, IUse, IInteractUsing
    {
        public override FireRateSelector FireRateSelector => _fireRateSelector;
        private FireRateSelector _fireRateSelector;
        public override FireRateSelector AllRateSelectors => _allRateSelectors;
        private FireRateSelector _allRateSelectors;
        public override float FireRate => _fireRate;
        private float _fireRate;
        
        private IGameTiming _gameTiming;
        // _lastFire is when we actually fired (so if we hold the button then recoil doesn't build up if we're not firing)
        private TimeSpan _lastFire;
        
        public abstract IEntity PeekAmmo();
        public abstract IEntity TakeProjectile();

        // Casing ejection
        private const float BulletEjectOffset = 0.2f;
        [Dependency] private IRobustRandom _robustRandom;
        private readonly Direction[] _randomBulletDirs =
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West
        };

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
        private SoundComponent _soundComponent;

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
            serializer.DataField(ref _allRateSelectors, "allSelectors", FireRateSelector.Safety);
            serializer.DataField(ref _fireRate, "fireRate", 2.0f);

            // This hard-to-read area's dealing with recoil
            // Use degrees in yaml as it's easier to read compared to "0.0125f"
            var minAngle = serializer.ReadDataField("minAngle", 5) / 2;
            _minAngle = Angle.FromDegrees(minAngle);
            // Random doubles it as it's +/- so uhh we'll just half it here for readability 
            var maxAngle = serializer.ReadDataField("maxAngle", 45) / 2;
            _maxAngle = Angle.FromDegrees(maxAngle);
            var angleIncrease = serializer.ReadDataField("angleIncrease", (100 / _fireRate));
            _angleIncrease = angleIncrease * (float) Math.PI / 180;
            var angleDecay = serializer.ReadDataField("angleDecay", (float) _maxAngle.Theta * 2);
            _angleDecay = angleDecay * (float) Math.PI / 180;
            serializer.DataField(ref _spreadRatio, "ammoSpreadRatio", 1.0f);
            
            // For simplicity we'll enforce it this way; ammo determines max spread
            if (_spreadRatio > 1.0f)
            {
                Logger.Error("SpreadRatio must be <= 1.0f for guns");
                throw new InvalidOperationException();
            }

            serializer.DataField(ref _canMuzzleFlash, "canMuzzleFlash", true);
            // Sounds
            serializer.DataField(ref _soundGunshot, "soundGunshot", null);
            serializer.DataField(ref _soundEmpty, "soundEmpty", "/Audio/Guns/Empty/empty.ogg");

            // Validate yaml
            if ((_fireRateSelector & _allRateSelectors) == 0)
            {
                Logger.Error($"Set an invalid FireRateSelector for {Name}");
                throw new InvalidOperationException();
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            _gameTiming = IoCManager.Resolve<IGameTiming>();
            if (Owner.TryGetComponent(out SoundComponent soundComponent))
            {
                _soundComponent = soundComponent;
            }
        }

        public override void OnAdd()
        {
            base.OnAdd();
            var rangedWeapon = Owner.GetComponent<ServerRangedWeaponComponent>();
            rangedWeapon.Barrel = this;
            rangedWeapon.FireHandler += Fire;
            rangedWeapon.WeaponCanFireHandler += WeaponCanFire;
        }

        public override void OnRemove()
        {
            base.OnRemove();
            var rangedWeapon = Owner.GetComponent<ServerRangedWeaponComponent>();
            rangedWeapon.Barrel = null;
            rangedWeapon.FireHandler -= Fire;
            rangedWeapon.WeaponCanFireHandler -= WeaponCanFire;
        }

        private Angle GetRecoilAngle(Angle direction)
        {
            var currentTime = _gameTiming.CurTime;
            var timeSinceLastFire = (currentTime - _lastFire).TotalSeconds;
            var newTheta = Math.Clamp(_currentAngle.Theta + _angleIncrease - _angleDecay * timeSinceLastFire, _minAngle.Theta, _maxAngle.Theta);
            _currentAngle = new Angle(newTheta);

            var random = (_robustRandom.NextDouble() - 0.5) * 2;
            var angle = Angle.FromDegrees(direction.Degrees + _currentAngle.Degrees * random);
            return angle;
        }

        public abstract bool UseEntity(UseEntityEventArgs eventArgs);
        public abstract bool InteractUsing(InteractUsingEventArgs eventArgs);

        public void ChangeFireSelector(FireRateSelector rateSelector)
        {
            if ((rateSelector & _allRateSelectors) != 0)
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

        private void Fire(IEntity shooter, GridCoordinates target)
        {
            if (ShotsLeft == 0)
            {
                _soundComponent?.Play(_soundEmpty);
                return;
            }

            var ammo = PeekAmmo();
            var projectile = TakeProjectile();
            if (projectile == null)
            {
                _soundComponent?.Play(_soundEmpty);
                return;
            }

            // At this point firing is confirmed
            var worldPosition = IoCManager.Resolve<IMapManager>().GetGrid(target.GridID).LocalToWorld(target).Position;
            var direction = (worldPosition - shooter.Transform.WorldPosition).ToAngle();
            var angle = GetRecoilAngle(direction);

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
                    ammoComponent.MuzzleFlash(Owner.Transform.GridPosition, angle);
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
            
            _soundComponent?.Play(_soundGunshot);
            _lastFire = _gameTiming.CurTime;

            return;
        }

        protected void EjectCasing(IEntity entity)
        {
            var ammo = entity.GetComponent<AmmoComponent>();
            var offsetPos = (CalcBulletOffset(), CalcBulletOffset());
            entity.Transform.GridPosition = Owner.Transform.GridPosition.Offset(offsetPos);
            entity.Transform.LocalRotation = _robustRandom.Pick(_randomBulletDirs).ToAngle();

            if (ammo.SoundCollectionEject == null)
            {
                return;
            }
            
            var soundCollection = IoCManager.Resolve<IPrototypeManager>().Index<SoundCollectionPrototype>(ammo.SoundCollectionEject);
            var randomFile = _robustRandom.Pick(soundCollection.PickFiles);
            _soundComponent?.Play(randomFile, AudioParams.Default.WithVolume(-1));
            
        }

        private float CalcBulletOffset()
        {
            return _robustRandom.NextFloat() * (BulletEjectOffset * 2) - BulletEjectOffset;
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
                        Owner.EntityManager.SpawnEntity(baseProjectile.Prototype.ID, Owner.Transform.GridPosition);
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

                var physicsComponent = projectile.GetComponent<PhysicsComponent>();
                physicsComponent.Status = BodyStatus.InAir;
                projectile.Transform.GridPosition = Owner.Transform.GridPosition;
                
                var projectileComponent = projectile.GetComponent<ProjectileComponent>();
                projectileComponent.IgnoreEntity(shooter);
                projectile.GetComponent<PhysicsComponent>().LinearVelocity = projectileAngle.ToVec() * velocity;
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
            var ray = new CollisionRay(Owner.Transform.GridPosition.Position, angle.ToVec(), (int) hitscan.CollisionMask);
            var physicsManager = IoCManager.Resolve<IPhysicsManager>();
            var rayCastResults = physicsManager.IntersectRay(Owner.Transform.MapID, ray, hitscan.MaxLength, shooter);
            var firstResult = rayCastResults.ToArray()[0];

            if (firstResult.HitEntity != null &&
                firstResult.HitEntity.TryGetComponent(out DamageableComponent damageableComponent))
            {
                damageableComponent.TakeDamage(
                    hitscan.DamageType, 
                    (int)Math.Round(hitscan.Damage, MidpointRounding.AwayFromZero), 
                    Owner, 
                    shooter);
                //I used Math.Round over Convert.toInt32, as toInt32 always rounds to
                //even numbers if halfway between two numbers, rather than rounding to nearest
            }
            
            hitscan.FireEffects(shooter, firstResult, angle);
        }
        #endregion
    }
}
