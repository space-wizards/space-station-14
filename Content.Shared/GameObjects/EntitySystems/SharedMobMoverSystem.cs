#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.GameObjects.EntitySystems
{
    public sealed class SharedMobMoverSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CollisionMessage>(HandleCollisionMessage);
        }

        private void HandleCollisionMessage(CollisionMessage message)
        {
            if (message.OurBody.BodyType != BodyType.KinematicController ||
                message.OtherBody.BodyType != BodyType.Dynamic ||
                !message.OurBody.Entity.TryGetComponent(out IMobMoverComponent? mobMover) ||
                message.Manifold.LocalNormal == Vector2.Zero) return;

            message.OtherBody.ApplyLinearImpulse(-message.Manifold.LocalNormal * mobMover.PushStrength * message.FrameTime);
        }
    }
}
