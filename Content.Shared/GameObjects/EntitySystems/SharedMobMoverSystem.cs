#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;

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
        private void HandleCollisionMessage(Fixture ourFixture, Fixture otherFixture, float frameTime, Manifold manifold)
        {
            var otherBody = otherFixture.Body;

            if (otherBody.BodyType != BodyType.Dynamic || !otherFixture.Hard) return;

            var normal = manifold.LocalNormal;

            if (!ourFixture.Body.Owner.TryGetComponent(out IMobMoverComponent? mobMover) || normal == Vector2.Zero) return;

            otherBody.ApplyLinearImpulse(-normal * mobMover.PushStrength * frameTime);
        }
    }
}
