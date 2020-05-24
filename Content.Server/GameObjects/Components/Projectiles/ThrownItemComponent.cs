using System.Collections.Generic;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    internal class ThrownItemComponent : ProjectileComponent, ICollideBehavior
    {
#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
#pragma warning restore 649

        public override string Name => "ThrownItem";

        /// <summary>
        ///     User who threw the item.
        /// </summary>
        public IEntity User;

        void ICollideBehavior.CollideWith(List<IEntity> collidedwith)
        {
            foreach (var entity in collidedwith)
            {
                if (entity.TryGetComponent(out DamageableComponent damage))
                {
                    damage.TakeDamage(DamageType.Brute, 10, Owner, User);
                }
            }

            // Stop colliding with mobs, this mimics not having enough velocity to do damage
            // after impacting the first object.
            // For realism this should actually be changed when the velocity of the object is less than a threshold.
            // This would allow ricochets off walls, and weird gravity effects from slowing the object.
            if (collidedwith.Count > 0 && Owner.TryGetComponent(out CollidableComponent body) && body.PhysicsShapes.Count >= 1)
            {
                body.PhysicsShapes[0].CollisionMask &= (int)~CollisionGroup.MobImpassable;
                body.IsScrapingFloor = true;

                // KYS, your job is finished. Trigger ILand as well.
                Owner.RemoveComponent<ThrownItemComponent>();
                _entitySystemManager.GetEntitySystem<InteractionSystem>().LandInteraction(User, Owner, Owner.Transform.GridPosition);
            }



        }
    }
}
