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
    public abstract class SharedMobMoverController : AetherController
    {
        private SharedBroadPhaseSystem _broadPhaseSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            _broadPhaseSystem = EntitySystem.Get<SharedBroadPhaseSystem>();
        }

        protected void UpdateKinematics(float frameTime, ITransformComponent transform, IMoverComponent mover, PhysicsComponent physicsComponent)
        {
            // TODO: Look at https://gameworksdocs.nvidia.com/PhysX/4.1/documentation/physxguide/Manual/CharacterControllers.html?highlight=controller as it has some adviceo n kinematic controllersx
            if (!UseMobMovement(_broadPhaseSystem, physicsComponent)) return;

            var (walkDir, sprintDir) = mover.VelocityDir;

            var weightless = transform.Owner.IsWeightless();

            // Handle wall-pushes.
            if (weightless)
            {
                // No gravity: is our entity touching anything?
                var touching = IsAroundCollider(_broadPhaseSystem, transform, mover, physicsComponent);

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
            }

            physicsComponent.LinearVelocity = total;
            HandleFootsteps(mover);
            return;

            /*
            var wishSpeed = total.Length;
            var wishDir = wishSpeed > 0 ? total.Normalized : Vector2.Zero;

            // Clamp to server-max speed?
            var accelerate = 40.0f;

            Accelerate(frameTime, physicsComponent, wishDir, wishSpeed, accelerate);

            if (wishSpeed != 0f)
            {
                transform.LocalRotation = total.GetDir().ToAngle();
            }

            // TODO: Conveyors should probably be a separate controller that adds to the basevelocity of the body (which gets reset every tick I think?)

            DebugTools.Assert(!float.IsNaN(physicsComponent.LinearVelocity.Length));
            HandleFootsteps(mover);

        }

        // Okay Touma
        private void Accelerate(float frameTime, PhysicsComponent body, Vector2 wishDir, float wishSpeed, float accel)
        {
            if (!ActionBlockerSystem.CanMove(body.Owner)) return;

            var currentSpeed = Vector2.Dot(body.LinearVelocity, wishDir);
            var addSpeed = wishSpeed - currentSpeed;

            if (addSpeed <= 0f) return;

            // TODO Look at source for dis.
            var accelSpeed = accel * frameTime * wishSpeed;
            accelSpeed = MathF.Min(accelSpeed, addSpeed);

            body.LinearVelocity += wishDir * accelSpeed;
            */
        }

        public static bool UseMobMovement(SharedBroadPhaseSystem broadPhaseSystem, PhysicsComponent body)
        {
            return body.Owner.HasComponent<IMobStateComponent>() &&
                   ActionBlockerSystem.CanMove(body.Owner) &&
                   (!body.Owner.IsWeightless() ||
                    body.Owner.TryGetComponent(out IMoverComponent? mover) &&
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
        public static bool IsAroundCollider(SharedBroadPhaseSystem broadPhaseSystem, ITransformComponent transform, IMoverComponent mover, IPhysicsComponent collider)
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

        protected virtual void HandleFootsteps(IMoverComponent mover) {}
    }
}
