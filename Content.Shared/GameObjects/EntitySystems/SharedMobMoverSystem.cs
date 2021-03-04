#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;

namespace Content.Shared.GameObjects.EntitySystems
{
    public sealed class SharedMobMoverSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            Get<SharedPhysicsSystem>().KinematicControllerCollision += HandleCollisionMessage;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            Get<SharedPhysicsSystem>().KinematicControllerCollision -= HandleCollisionMessage;
        }

        /// <summary>
        ///     Fake pushing for player collisions.
        /// </summary>
        private void HandleCollisionMessage(IPhysBody ourBody, IPhysBody otherBody, float frameTime, Manifold manifold)
        {
            if (otherBody.BodyType != BodyType.Dynamic) return;

            var normal = manifold.LocalNormal;

            if (!ourBody.Entity.TryGetComponent(out IMobMoverComponent? mobMover) || normal == Vector2.Zero) return;

            otherBody.ApplyLinearImpulse(-normal * mobMover.PushStrength * frameTime);
        }
    }
}
