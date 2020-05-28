using System.Collections.Generic;
using Content.Server.GameObjects.Components.Explosion;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Projectiles
{
    [RegisterComponent]
    public class ExplosiveProjectileComponent : Component, ICollideBehavior
    {
        public override string Name => "ExplosiveProjectile";

        public override void Initialize()
        {
            base.Initialize();
            if (!Owner.HasComponent<ProjectileComponent>())
            {
                Owner.AddComponent<ProjectileComponent>();
            }
        }

        public void CollideWith(List<IEntity> collidedwith)
        {
            // Projectile should just delete itself
            if (collidedwith.Count == 0)
            {
                return;
            }

            if (Owner.TryGetComponent(out ExplosiveComponent explosiveComponent))
            {
                explosiveComponent.Explosion();
            }
        }
    }
}