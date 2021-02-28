using Content.Server.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Projectiles
{
    /// <summary>
    /// Adds stun when it collides with an entity
    /// </summary>
    [RegisterComponent]
    public sealed class StunnableProjectileComponent : Component, ICollideBehavior
    {
        public override string Name => "StunnableProjectile";

        // See stunnable for what these do
        private int _stunAmount;
        private int _knockdownAmount;
        private int _slowdownAmount;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _stunAmount, "stunAmount", 0);
            serializer.DataField(ref _knockdownAmount, "knockdownAmount", 0);
            serializer.DataField(ref _slowdownAmount, "slowdownAmount", 0);
        }

        public override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn(out ProjectileComponent _);
        }

        void ICollideBehavior.CollideWith(IEntity entity)
        {
            if (entity.TryGetComponent(out StunnableComponent stunnableComponent))
            {
                stunnableComponent.Stun(_stunAmount);
                stunnableComponent.Knockdown(_knockdownAmount);
                stunnableComponent.Slowdown(_slowdownAmount);
            }
        }

        void ICollideBehavior.PostCollide(int collidedCount) {}
    }
}
