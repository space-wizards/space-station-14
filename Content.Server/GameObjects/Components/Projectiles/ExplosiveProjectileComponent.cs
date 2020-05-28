using System;
using Content.Server.GameObjects.Components.Explosion;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.Projectiles
{
    [RegisterComponent]
    public class ExplosiveProjectileComponent : Component, ICollideBehavior
    {
        public override string Name => "ExplosiveProjectile";

        public override void Initialize()
        {
            base.Initialize();
            if (!Owner.HasComponent<ExplosiveComponent>())
            {
                Logger.Error("ExplosiveProjectiles need an ExplosiveComponent");
                throw new InvalidOperationException();
            }
        }

        void ICollideBehavior.CollideWith(IEntity entity)
        {
            var explosiveComponent = Owner.GetComponent<ExplosiveComponent>();
            explosiveComponent.Explosion();
        }

        // Projectile should handle the deleting
        void ICollideBehavior.PostCollide(int collisionCount)
        {
            return;
        }
    }
}