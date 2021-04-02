using Content.Server.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Projectiles
{
    /// <summary>
    /// Adds stun when it collides with an entity
    /// </summary>
    [RegisterComponent]
    public sealed class StunnableProjectileComponent : Component, IStartCollide
    {
        public override string Name => "StunnableProjectile";

        // See stunnable for what these do
        [DataField("stunAmount")]
        private int _stunAmount = default;
        [DataField("knockdownAmount")]
        private int _knockdownAmount = default;
        [DataField("slowdownAmount")]
        private int _slowdownAmount = default;

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn(out ProjectileComponent _);
        }

        void IStartCollide.CollideWith(IPhysBody ourBody, IPhysBody otherBody, in Manifold manifold)
        {
            if (otherBody.Entity.TryGetComponent(out StunnableComponent? stunnableComponent))
            {
                stunnableComponent.Stun(_stunAmount);
                stunnableComponent.Knockdown(_knockdownAmount);
                stunnableComponent.Slowdown(_slowdownAmount);
            }
        }
    }
}
