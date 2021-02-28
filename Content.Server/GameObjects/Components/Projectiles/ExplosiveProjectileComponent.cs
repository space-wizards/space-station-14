using Content.Server.GameObjects.Components.Explosion;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

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

        void ICollideBehavior.CollideWith(IPhysBody ourBody, IPhysBody otherBody)
        {
            if (Owner.TryGetComponent(out ExplosiveComponent explosive))
            {
                explosive.Explosion();
            }
        }
    }
}
