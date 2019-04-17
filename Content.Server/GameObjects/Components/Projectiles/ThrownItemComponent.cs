using System.Collections.Generic;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Shared.GameObjects;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;

namespace Content.Server.GameObjects.Components
{
    class ThrownItemComponent : ProjectileComponent, ICollideBehavior
    {
        public override string Name => "ThrownItem";

        void ICollideBehavior.CollideWith(List<IEntity> collidedwith)
        {
            foreach (var entity in collidedwith)
            {
                if (entity.TryGetComponent(out DamageableComponent damage))
                {
                    damage.TakeDamage(DamageType.Brute, 10);
                }
            }

            // Stop colliding with mobs, this mimics not having enough velocity to do damage
            // after impacting the first object.
            // For realism this should actually be changed when the velocity of the object is less than a threshold.
            // This would allow ricochets off walls, and weird gravity effects from slowing the object.
            if (collidedwith.Count > 0 && Owner.TryGetComponent(out CollidableComponent body))
            {
                body.CollisionMask &= (int)~CollisionGroup.Mob;
                body.IsScrapingFloor = true;

                // KYS, your job is finished.
                Owner.RemoveComponent<ThrownItemComponent>();
            }
        }
    }
}
