#nullable enable
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Pulling;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Physics.Controllers
{
    public class SharedClearVelocityController : AetherController
    {
        public override void UpdateBeforeSolve(bool prediction, PhysicsMap map, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, map, frameTime);
            foreach (var body in map.AwakeBodies)
            {
                // TODO: Disable or not idfk
                continue;
                var useMobMovement = body.Owner.HasComponent<IMobStateComponent>() &&
                                     ActionBlockerSystem.CanMove(body.Owner) &&
                                     (!body.Owner.IsWeightless() ||
                                      body.Owner.TryGetComponent(out IMoverComponent? mover) &&
                                      IsAroundCollider(body.Owner.Transform, mover, body));

                if (useMobMovement)
                {
                    body.LinearVelocity = Vector2.Zero;
                }
            }
        }

        private bool IsAroundCollider(ITransformComponent transform, IMoverComponent mover,
            IPhysicsComponent collider)
        {
            var enlargedAABB = collider.GetWorldAABB().Enlarged(mover.GrabRange);

            foreach (var otherCollider in EntitySystem.Get<SharedBroadPhaseSystem>().GetCollidingEntities(transform.MapID, enlargedAABB))
            {
                if (otherCollider == collider) continue; // Don't try to push off of yourself!

                // Only allow pushing off of anchored things that have collision.
                if (otherCollider.BodyType != BodyType.Static ||
                    !otherCollider.CanCollide ||
                    ((collider.CollisionMask & otherCollider.CollisionLayer) == 0 &&
                     (otherCollider.CollisionMask & collider.CollisionLayer) == 0) ||
                    (otherCollider.Entity.TryGetComponent(out SharedPullableComponent? pullable) && pullable.BeingPulled))
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
