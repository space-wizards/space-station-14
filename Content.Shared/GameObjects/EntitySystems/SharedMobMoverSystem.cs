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

        /// <summary>
        ///     Fake pushing for player collisions.
        /// </summary>
        /// <param name="message"></param>
        private void HandleCollisionMessage(CollisionMessage message)
        {
            IPhysBody ourBody;
            IPhysBody otherBody;

            if (message.BodyA.BodyType == BodyType.KinematicController)
            {
                ourBody = message.BodyA;
                otherBody = message.BodyB;
            }
            else if (message.BodyB.BodyType == BodyType.KinematicController)
            {
                ourBody = message.BodyB;
                otherBody = message.BodyA;
            }
            else
            {
                return;
            }

            if (otherBody.BodyType != BodyType.Dynamic) return;

            var normal = message.Manifold.LocalNormal;

            if (!ourBody.Entity.TryGetComponent(out IMobMoverComponent? mobMover) || normal == Vector2.Zero) return;

            otherBody.ApplyLinearImpulse(-normal * mobMover.PushStrength * message.FrameTime);
        }
    }
}
