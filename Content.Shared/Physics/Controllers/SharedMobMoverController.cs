#nullable enable
using System;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Pulling;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Physics.Pull;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Utility;

namespace Content.Shared.Physics.Controllers
{
    /// <summary>
    ///     Handles player and NPC mob movement.
    ///     NPCs are handled server-side only.
    /// </summary>
    public abstract class SharedMobMoverController : AetherController
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;

        private SharedBroadPhaseSystem _broadPhaseSystem = default!;

        private const float MobAcceleration = 10.0f;

        private const float MobFriction = 6.0f;

        public override void Initialize()
        {
            base.Initialize();
            _broadPhaseSystem = EntitySystem.Get<SharedBroadPhaseSystem>();
        }

        protected void UpdateKinematics(float frameTime, ITransformComponent transform, IMoverComponent mover, PhysicsComponent physicsComponent)
        {
            if (!ActionBlockerSystem.CanMove(mover.Owner)) return;

            // TODO: Fuck it's a hack but I want collisions working first
            if (mover.Owner.Prototype?.ID == "AdminObserver")
            {
                physicsComponent.LinearVelocity = Vector2.Zero;
            }

            var (walkDir, sprintDir) = mover.VelocityDir;

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
            var wishSpeed = total.Length;
            var wishDir = wishSpeed > 0 ? total.Normalized : Vector2.Zero;

            // Clamp to server-max speed?
            var accelerate = 80.0f;

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
            var accelSpeed = accel * frameTime * wishSpeed * (1 - GetTileFriction(body));
            accelSpeed = MathF.Min(accelSpeed, addSpeed);

            body.LinearVelocity += wishDir * accelSpeed;
        }

        private bool IsAroundCollider(ITransformComponent transform, IMoverComponent mover,
            IPhysicsComponent collider)
        {
            var enlargedAABB = collider.GetWorldAABB().Enlarged(mover.GrabRange);

            foreach (var otherCollider in _broadPhaseSystem.GetCollidingEntities(transform.MapID, enlargedAABB))
            {
                if (otherCollider == collider) continue; // Don't try to push off of yourself!

                // Only allow pushing off of anchored things that have collision.
                if (otherCollider.BodyType == BodyType.Static ||
                    !otherCollider.CanCollide ||
                    (collider.CollisionMask & otherCollider.CollisionLayer) == 0 ||
                    (otherCollider.CollisionMask & collider.CollisionLayer) == 0 ||
                    (otherCollider.Entity.TryGetComponent(out SharedPullableComponent? pullable) && pullable.BeingPulled))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        protected virtual void HandleFootsteps(IMoverComponent mover) {}

        // TODO: Copy-pasted shitcode.
        [Pure]
        private float GetTileFriction(IPhysicsComponent body)
        {
            if (!body.OnGround)
                return 0.0f;

            var transform = body.Owner.Transform;
            var coords = transform.Coordinates;

            var grid = _mapManager.GetGrid(coords.GetGridId(body.Owner.EntityManager));
            var tile = grid.GetTileRef(coords);
            var tileDef = _tileDefinitionManager[tile.Tile.TypeId];
            return tileDef.Friction;
        }
    }
}
