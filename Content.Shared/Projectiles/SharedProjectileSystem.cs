using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Projectiles
{
    public abstract class SharedProjectileSystem : EntitySystem
    {
        public const string ProjectileFixture = "projectile";

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SharedProjectileComponent, PreventCollideEvent>(PreventCollision);
        }

        private void PreventCollision(EntityUid uid, SharedProjectileComponent component, PreventCollideEvent args)
        {
            if (component.IgnoreShooter && args.BodyB.Owner == component.Shooter)
            {
                args.Cancel();
                return;
            }
        }
    }
}
