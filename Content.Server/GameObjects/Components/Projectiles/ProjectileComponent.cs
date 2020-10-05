#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Projectiles;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Projectiles
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedProjectileComponent))]
    public class ProjectileComponent : SharedProjectileComponent, ICollideBehavior
    {
        [ViewVariables]
        public Dictionary<DamageType, int> Damages { get; set; } = new Dictionary<DamageType, int>();

        private bool DeleteOnCollide { get; set; } = true;

        // Get that juicy FPS hit sound
        private string? _soundHit;
        private string? _soundHitSpecies;

        private bool _damagedEntity;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataReadWriteFunction("deleteOnCollide", DeleteOnCollide, value => DeleteOnCollide = value, () => DeleteOnCollide);
            // If not specified 0 damage
            serializer.DataReadWriteFunction("damages", Damages, value => Damages = value, () => Damages);
            serializer.DataField(ref _soundHit, "soundHit", null);
            serializer.DataField(ref _soundHitSpecies, "soundHitSpecies", null);
        }

        public float TimeLeft { get; set; } = 10;

        /// <summary>
        /// Function that makes the collision of this object ignore a specific entity so we don't collide with ourselves
        /// </summary>
        /// <param name="shooter"></param>
        public override void IgnoreEntity(IEntity shooter)
        {
            base.IgnoreEntity(shooter);
            Dirty();
        }

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
            if (entity.TryGetComponent(out ICollidableComponent? collidable) && !collidable.Hard)
            {
                DeleteOnCollide = false;
                return;
            }

            DeleteOnCollide = true;

            if (entity.TryGetComponent(out IDamageableComponent? damage) && _soundHitSpecies != null)
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundHitSpecies, entity.Transform.Coordinates);
            } else if (_soundHit != null)
            {
                EntitySystem.Get<AudioSystem>().PlayAtCoords(_soundHit, entity.Transform.Coordinates);
            }

            if (damage != null)
            {
                IEntity? shooter = null;
                
                if (Shooter != null)
                    Owner.EntityManager.TryGetEntity(Shooter.Value, out shooter);

                foreach (var (damageType, amount) in Damages)
                {
                    damage.ChangeDamage(damageType, amount, false, shooter);
                }

                _damagedEntity = true;
            }

            if (!entity.Deleted && entity.TryGetComponent(out CameraRecoilComponent? recoilComponent)
                                && Owner.TryGetComponent(out ICollidableComponent? collidableComponent))
            {
                var direction = collidableComponent.LinearVelocity.Normalized;
                recoilComponent.Kick(direction);
            }
        }

        void ICollideBehavior.PostCollide(int collideCount)
        {
            if (collideCount > 0 && DeleteOnCollide) Owner.Delete();
        }

        public override ComponentState GetComponentState()
        {
            return new ProjectileComponentState(NetID!.Value, Shooter, IgnoreShooter);
        }
    }
}
