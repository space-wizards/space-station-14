#nullable enable
using System;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Physics.Pull;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Controllers;

namespace Content.Shared.Physics.Controllers
{
    /// <summary>
    ///     Handles player and NPC mob movement.
    ///     NPCs are handled server-side only.
    /// </summary>
    public abstract class SharedMobMoverController : AetherController
    {
        [Dependency] private readonly IPhysicsManager _physicsManager = default!;

        private float _acceleration = 150.0f;

        protected void UpdateKinematics(float frameTime, ITransformComponent transform, IMoverComponent mover, PhysicsComponent physicsComponent)
        {
            if (!ActionBlockerSystem.CanMove(mover.Owner)) return;

            var (walkDir, sprintDir) = mover.VelocityDir;
            var combined = walkDir + sprintDir;

            var weightless = transform.Owner.IsWeightless();

            // Handle wall-pushes.
            if (weightless)
            {
                // No gravity: is our entity touching anything?
                var touching = IsAroundCollider(transform, mover, physicsComponent);

                if (!touching)
                {
                    transform.LocalRotation = physicsComponent.LinearVelocity.GetDir().ToAngle();
                }
                else
                {
                    // Controller.Push(combined, mover.CurrentSpeed)
                }

                return;
            }

            // Regular movement.
            var total = (walkDir * mover.CurrentWalkSpeed + sprintDir * mover.CurrentSprintSpeed);

            if (total != Vector2.Zero)
            {
                Accelerate(frameTime, physicsComponent, total);
                transform.LocalRotation = total.GetDir().ToAngle();
            }

            // TODO: Like I said on PhysicsIsland damping is megasketch. Just to make players feel better to play
            // we'll use our own friction here coz fuck it why not
            Friction(frameTime, physicsComponent, total);

            HandleFootsteps(mover);
        }

        private bool IsAroundCollider(ITransformComponent transform, IMoverComponent mover,
            IPhysicsComponent collider)
        {
            // TODO: Should use physics lookups instead via broadPhase.
            foreach (var entity in EntityManager.GetEntitiesInRange(transform.Owner, mover.GrabRange, true))
            {
                if (entity == transform.Owner)
                {
                    continue; // Don't try to push off of yourself!
                }

                // Only allow pushing off of anchored things that have collision.
                if (!entity.TryGetComponent<IPhysicsComponent>(out var otherCollider) ||
                    otherCollider.BodyType == BodyType.Static ||
                    !otherCollider.CanCollide ||
                    (collider.CollisionMask & otherCollider.CollisionLayer) == 0 ||
                    (otherCollider.CollisionMask & collider.CollisionLayer) == 0 ||
                    otherCollider.HasController<PullController>())
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        // Okay Touma
        private void Accelerate(float frameTime, PhysicsComponent physicsComponent, Vector2 wishDir)
        {
            var acceleration = 10.0f;

            var wishSpeed = wishDir.Length;
            var currentSpeed = Vector2.Dot(physicsComponent.LinearVelocity, wishDir.Normalized);
            var addSpeed = wishSpeed - currentSpeed;

            if (addSpeed <= 0f) return;

            var accelSpeed = acceleration * frameTime * wishSpeed;
            accelSpeed = MathF.Min(accelSpeed, addSpeed);

            physicsComponent.LinearVelocity += wishDir.Normalized * accelSpeed;
        }

        /// <summary>
        ///     Artificial player friction to make movement feel snappier.
        /// </summary>
        /// <param name="frameTime"></param>
        /// <param name="physicsComponent"></param>
        /// <param name="wishDir"></param>
        private void Friction(float frameTime, PhysicsComponent physicsComponent, Vector2 wishDir)
        {
            // If we have no control can't slow our movement down then.
            if (!ActionBlockerSystem.CanMove(physicsComponent.Owner) || physicsComponent.LinearVelocity == Vector2.Zero) return;

            var friction = physicsComponent.LinearVelocity.Normalized * frameTime * 40;
            if (friction.Length > physicsComponent.LinearVelocity.Length)
            {
                friction = friction.Normalized * physicsComponent.LinearVelocity.Length;
            }

            physicsComponent.LinearVelocity -= friction;
        }

        protected virtual void HandleFootsteps(IMoverComponent mover) {}
    }
}
