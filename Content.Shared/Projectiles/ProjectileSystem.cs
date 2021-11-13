using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Projectiles
{
    public sealed class ProjectileSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedProjectileComponent, PreventCollideEvent>(PreventCollision);
        }

        private void PreventCollision(EntityUid uid, SharedProjectileComponent component, PreventCollideEvent args)
        {
            if (component.IgnoreShooter && args.BodyB.OwnerUid == component.Shooter)
            {
                args.Cancel();
                return;
            }
        }
    }
}
