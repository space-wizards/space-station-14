using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Projectiles
{
    [RegisterComponent]
    public class ProjectileComponent : Component, ICollideSpecial, ICollideBehavior
    {
        public override string Name => "Projectile";

        public bool IgnoreShooter = true;

        private EntityUid Shooter = EntityUid.Invalid;

        private Dictionary<DamageType, int> _damages;
        [ViewVariables]
        public Dictionary<DamageType, int> Damages => _damages;
        private float _velocity;
        public float Velocity
        {
            get => _velocity;
            set => _velocity = value;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            // If not specified 0 damage
            serializer.DataField(ref _damages, "damages", new Dictionary<DamageType, int>());
            serializer.DataField(ref _velocity, "velocity", 20f);
        }

        public float TimeLeft { get; set; } = 10;

        /// <summary>
        /// Function that makes the collision of this object ignore a specific entity so we don't collide with ourselves
        /// </summary>
        /// <param name="shooter"></param>
        public void IgnoreEntity(IEntity shooter)
        {
            Shooter = shooter.Uid;
        }

        /// <summary>
        /// Special collision override, can be used to give custom behaviors deciding when to collide
        /// </summary>
        /// <param name="collidedwith"></param>
        /// <returns></returns>
        bool ICollideSpecial.PreventCollide(IPhysBody collidedwith)
        {
            if (IgnoreShooter && collidedwith.Owner.Uid == Shooter)
                return true;
            return false;
        }

        /// <summary>
        /// Applys the damage when our projectile collides with its victim
        /// </summary>
        /// <param name="collidedwith"></param>
        void ICollideBehavior.CollideWith(List<IEntity> collidedwith)
        {
            foreach (var entity in collidedwith)
            {
                if (entity.TryGetComponent(out DamageableComponent damage))
                {
                    Owner.EntityManager.TryGetEntity(Shooter, out var shooter);

                    foreach (var (damageType, amount) in _damages)
                    {

                        damage.TakeDamage(damageType, amount, Owner, shooter);
                    }
                }

                if (!entity.Deleted && entity.TryGetComponent(out CameraRecoilComponent recoilComponent)
                    && Owner.TryGetComponent(out PhysicsComponent physicsComponent))
                {
                    var direction = physicsComponent.LinearVelocity.Normalized;
                    recoilComponent.Kick(direction);
                }
            }

            if (collidedwith.Count > 0)
            {
                Owner.Delete();
            }
        }
    }
}
