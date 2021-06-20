using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public class ExplosiveProjectileComponent : Component, IStartCollide
    {
        public override string Name => "ExplosiveProjectile";

        protected override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponent<ExplosiveComponent>();
        }

        void IStartCollide.CollideWith(Fixture ourFixture, Fixture otherFixture, in Manifold manifold)
        {
            if (Owner.TryGetComponent(out ExplosiveComponent? explosive))
            {
                explosive.Explosion();
            }
        }
    }
}
