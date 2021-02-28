using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Projectiles;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Projectiles
{
    [RegisterComponent]
    public class ProjectileComponent : SharedProjectileComponent, ICollideBehavior, IPostCollide
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

        private bool _damagedEntity = false;

        private bool _deleteOnCollide;

        // Get that juicy FPS hit sound
        private string _soundHit;
        private string _soundHitSpecies;

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

        /// <summary>
        ///     Applies the damage when our projectile collides with its victim
        /// </summary>
        void ICollideBehavior.CollideWith(IPhysBody ourBody, IPhysBody otherBody)
        {
            // This is so entities that shouldn't get a collision are ignored.
            if (!otherBody.Hard || _damagedEntity)
            {
                return;
            }

            if (otherBody.Entity.TryGetComponent(out IDamageableComponent damage) && _soundHitSpecies != null)
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundHitSpecies, otherBody.Entity.Transform.Coordinates);
            }
            else if (_soundHit != null)
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundHit, otherBody.Entity.Transform.Coordinates);
            }

            if (damage != null)
            {
                Owner.EntityManager.TryGetEntity(_shooter, out var shooter);

                foreach (var (damageType, amount) in _damages)
                {
                    damage.ChangeDamage(damageType, amount, false, shooter);
                }

                _damagedEntity = true;
            }

            // Damaging it can delete it
            if (!otherBody.Entity.Deleted && otherBody.Entity.TryGetComponent(out CameraRecoilComponent recoilComponent))
            {
                var direction = ourBody.LinearVelocity.Normalized;
                recoilComponent.Kick(direction);
            }
        }

        void IPostCollide.PostCollide(IPhysBody ourBody, IPhysBody otherBody)
        {
            if (_damagedEntity) Owner.Delete();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new ProjectileComponentState(NetID!.Value, _shooter, IgnoreShooter);
        }
    }
}
