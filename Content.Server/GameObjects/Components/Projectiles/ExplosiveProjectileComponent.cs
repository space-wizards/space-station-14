using Content.Server.GameObjects.Components.Explosion;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Projectiles
{
    [RegisterComponent]
    public class ExplosiveProjectileComponent : Component, ICollideBehavior
    {
        public override string Name => "ExplosiveProjectile";

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponent<ExplosiveComponent>();
        }

        void ICollideBehavior.CollideWith(IEntity entity)
        {
            if (Owner.TryGetComponent(out ExplosiveComponent explosive))
            {
                explosive.Explosion();
            }
        }

        // Projectile should handle the deleting
        void ICollideBehavior.PostCollide(int collisionCount)
        {
            return;
        }
    }
}
