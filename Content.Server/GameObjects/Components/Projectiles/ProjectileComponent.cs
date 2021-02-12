using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Projectiles;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Projectiles
{
    [RegisterComponent]
    public class ProjectileComponent : SharedProjectileComponent, ICollideBehavior
    {
        protected override EntityUid Shooter => _shooter;

        private EntityUid _shooter = EntityUid.Invalid;

        private Dictionary<DamageType, int> _damages;

        [ViewVariables]
        public Dictionary<DamageType, int> Damages
        {
            get => _damages;
            set => _damages = value;
        }

        public bool DeleteOnCollide => _deleteOnCollide;
        private bool _deleteOnCollide;

        // Get that juicy FPS hit sound
        private string _soundHit;
        private string _soundHitSpecies;

        private bool _damagedEntity;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _deleteOnCollide, "delete_on_collide", true);
            // If not specified 0 damage
            serializer.DataField(ref _damages, "damages", new Dictionary<DamageType, int>());
            serializer.DataField(ref _soundHit, "soundHit", null);
            serializer.DataField(ref _soundHitSpecies, "soundHitSpecies", null);
        }

        public float TimeLeft { get; set; } = 10;

        /// <summary>
        /// Function that makes the collision of this object ignore a specific entity so we don't collide with ourselves
        /// </summary>
        /// <param name="shooter"></param>
        public void IgnoreEntity(IEntity shooter)
        {
            _shooter = shooter.Uid;
            Dirty();
        }

        private bool _internalDeleteOnCollide;

        /// <summary>
        /// Applies the damage when our projectile collides with its victim
        /// </summary>
        /// <param name="entity"></param>
        void ICollideBehavior.CollideWith(IEntity entity)
        {
            if (_damagedEntity)
            {
                return;
            }

            // This is so entities that shouldn't get a collision are ignored.
            if (entity.TryGetComponent(out IPhysicsComponent otherPhysics) && otherPhysics.Hard == false)
            {
                _internalDeleteOnCollide = false;
                return;
            }
            else
            {
                _internalDeleteOnCollide = true;
            }

            if (_soundHitSpecies != null && entity.HasComponent<IDamageableComponent>())
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundHitSpecies, entity.Transform.Coordinates);
            } else if (_soundHit != null)
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundHit, entity.Transform.Coordinates);
            }

            if (entity.TryGetComponent(out IDamageableComponent damage))
            {
                Owner.EntityManager.TryGetEntity(_shooter, out var shooter);

                foreach (var (damageType, amount) in _damages)
                {
                    damage.ChangeDamage(damageType, amount, false, shooter);
                }

                _damagedEntity = true;
            }

            if (!entity.Deleted && entity.TryGetComponent(out CameraRecoilComponent recoilComponent)
                                && Owner.TryGetComponent(out IPhysicsComponent ownPhysics))
            {
                var direction = ownPhysics.LinearVelocity.Normalized;
                recoilComponent.Kick(direction);
            }
        }

        void ICollideBehavior.PostCollide(int collideCount)
        {
            if (collideCount > 0 && DeleteOnCollide && _internalDeleteOnCollide) Owner.Delete();
        }

        public override ComponentState GetComponentState()
        {
            return new ProjectileComponentState(NetID!.Value, _shooter, IgnoreShooter);
        }
    }
}
