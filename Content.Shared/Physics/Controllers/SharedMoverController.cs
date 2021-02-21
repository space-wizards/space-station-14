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

namespace Content.Shared.Physics.Controllers
{
    /// <summary>
    ///     Handles player and NPC mob movement.
    ///     NPCs are handled server-side only.
    /// </summary>
    public abstract class SharedMoverController : AetherController
    {
        private SharedBroadPhaseSystem _broadPhaseSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            _broadPhaseSystem = EntitySystem.Get<SharedBroadPhaseSystem>();
        }

        /// <summary>
        ///     A generic kinematic mover for entities.
        /// </summary>
        protected void HandleKinematicMovement(IMoverComponent mover, PhysicsComponent physicsComponent)
        {
            var (walkDir, sprintDir) = mover.VelocityDir;

            // Regular movement.
            // Target velocity.
            var total = (walkDir * mover.CurrentWalkSpeed + sprintDir * mover.CurrentSprintSpeed);

            if (total != Vector2.Zero)
            {
                mover.Owner.Transform.LocalRotation = total.GetDir().ToAngle();
            }

            physicsComponent.LinearVelocity = total;
        }

        // TODO: Shuttle mover here
        /*
         * Some thoughts:
         * Unreal actually doesn't predict vehicle movement at all, it's purely server-side which I thought was interesting
         * The reason for this is that vehicles change direction very slowly compared to players so you don't really have the requirement for quick movement anyway
         * As such could probably just look at applying a force / impulse to the shuttle server-side only so it controls like the titanic.
         */

        /// <summary>
        ///     Movement while considering actionblockers, weightlessness, etc.
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="mover"></param>
        /// <param name="physicsComponent"></param>
        protected void HandleMobMovement(IMoverComponent mover, PhysicsComponent physicsComponent, IMobMoverComponent mobMover)
        {
            // TODO: Look at https://gameworksdocs.nvidia.com/PhysX/4.1/documentation/physxguide/Manual/CharacterControllers.html?highlight=controller as it has some adviceo n kinematic controllersx
            if (!UseMobMovement(_broadPhaseSystem, physicsComponent)) return;

            var transform = mover.Owner.Transform;
            var (walkDir, sprintDir) = mover.VelocityDir;

            var weightless = transform.Owner.IsWeightless();

            // Handle wall-pushes.
            if (weightless)
            {
                // No gravity: is our entity touching anything?
                var touching = IsAroundCollider(_broadPhaseSystem, transform, mobMover, physicsComponent);

                if (!touching)
                {
                    transform.LocalRotation = physicsComponent.LinearVelocity.GetDir().ToAngle();
                    return;
                }
            }

            // Regular movement.
            // Target velocity.
            var total = (walkDir * mover.CurrentWalkSpeed + sprintDir * mover.CurrentSprintSpeed);

            if (total != Vector2.Zero)
            {
                transform.LocalRotation = total.GetDir().ToAngle();
                HandleFootsteps(mover, mobMover);
            }

            physicsComponent.LinearVelocity = total;
        }

        public static bool UseMobMovement(SharedBroadPhaseSystem broadPhaseSystem, PhysicsComponent body)
        {
            return (body.Status == BodyStatus.OnGround) &
                   body.Owner.HasComponent<IMobStateComponent>() &&
                   ActionBlockerSystem.CanMove(body.Owner) &&
                   (!body.Owner.IsWeightless() ||
                    body.Owner.TryGetComponent(out SharedPlayerMobMoverComponent? mover) &&
                    IsAroundCollider(broadPhaseSystem, body.Owner.Transform, mover, body));
        }

        /// <summary>
        ///     Used for weightlessness to determine if we are near a wall.
        /// </summary>
        /// <param name="broadPhaseSystem"></param>
        /// <param name="transform"></param>
        /// <param name="mover"></param>
        /// <param name="collider"></param>
        /// <returns></returns>
        public static bool IsAroundCollider(SharedBroadPhaseSystem broadPhaseSystem, ITransformComponent transform, IMobMoverComponent mover, IPhysicsComponent collider)
        {
            var enlargedAABB = collider.GetWorldAABB().Enlarged(mover.GrabRange);

            foreach (var otherCollider in broadPhaseSystem.GetCollidingEntities(transform.MapID, enlargedAABB))
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

        // TODO: Need a predicted client version that only plays for our own entity and then have server-side ignore our session (for that entity only)
        protected virtual void HandleFootsteps(IMoverComponent mover, IMobMoverComponent mobMover) {}
    }
}
