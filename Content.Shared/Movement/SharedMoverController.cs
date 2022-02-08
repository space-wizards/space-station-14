using System.Collections.Generic;
using Content.Shared.ActionBlocker;
using Content.Shared.CCVar;
using Content.Shared.Friction;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Pulling.Components;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Utility;

namespace Content.Shared.Movement
{
    /// <summary>
    ///     Handles player and NPC mob movement.
    ///     NPCs are handled server-side only.
    /// </summary>
    public abstract class SharedMoverController : VirtualController
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        [Dependency] private ActionBlockerSystem _blocker = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;

        private bool _relativeMovement;

        /// <summary>
        /// Cache the mob movement calculation to re-use elsewhere.
        /// </summary>
        public Dictionary<EntityUid, bool> UsedMobMovement = new();

        public override void Initialize()
        {
            base.Initialize();
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CCVars.RelativeMovement, SetRelativeMovement, true);
            UpdatesBefore.Add(typeof(SharedTileFrictionController));
        }

        private void SetRelativeMovement(bool value) => _relativeMovement = value;

        public override void Shutdown()
        {
            base.Shutdown();
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.UnsubValueChanged(CCVars.RelativeMovement, SetRelativeMovement);
        }

        public override void UpdateAfterSolve(bool prediction, float frameTime)
        {
            base.UpdateAfterSolve(prediction, frameTime);
            UsedMobMovement.Clear();
        }

        protected Angle GetParentGridAngle(TransformComponent xform, IMoverComponent mover)
        {
            if (xform.GridID == GridId.Invalid || !_mapManager.TryGetGrid(xform.GridID, out var grid))
                return mover.LastGridAngle;

            return grid.WorldRotation;
        }

        /// <summary>
        ///     A generic kinematic mover for entities.
        /// </summary>
        protected void HandleKinematicMovement(IMoverComponent mover, PhysicsComponent physicsComponent)
        {
            var (walkDir, sprintDir) = mover.VelocityDir;

            var transform = EntityManager.GetComponent<TransformComponent>(mover.Owner);
            var parentRotation = GetParentGridAngle(transform, mover);

            // Regular movement.
            // Target velocity.
            var total = walkDir * mover.CurrentWalkSpeed + sprintDir * mover.CurrentSprintSpeed;

            var worldTotal = _relativeMovement ? parentRotation.RotateVec(total) : total;

            if (transform.GridID != GridId.Invalid)
                mover.LastGridAngle = parentRotation;

            if (worldTotal != Vector2.Zero)
                transform.WorldRotation = worldTotal.GetDir().ToAngle();

            _physics.SetLinearVelocity(physicsComponent, worldTotal);
        }

        /// <summary>
        ///     Movement while considering actionblockers, weightlessness, etc.
        /// </summary>
        protected void HandleMobMovement(
            IMoverComponent mover,
            PhysicsComponent physicsComponent,
            IMobMoverComponent mobMover,
            TransformComponent xform)
        {
            DebugTools.Assert(!UsedMobMovement.ContainsKey(mover.Owner));

            if (!UseMobMovement(physicsComponent))
            {
                UsedMobMovement[mover.Owner] = false;
                return;
            }

            UsedMobMovement[mover.Owner] = true;
            var weightless = mover.Owner.IsWeightless(physicsComponent, mapManager: _mapManager, entityManager: EntityManager);
            var (walkDir, sprintDir) = mover.VelocityDir;

            // Handle wall-pushes.
            if (weightless)
            {
                // No gravity: is our entity touching anything?
                var touching = IsAroundCollider(_physics, xform, mobMover, physicsComponent);

                if (!touching)
                {
                    if (xform.GridID != GridId.Invalid)
                        mover.LastGridAngle = GetParentGridAngle(xform, mover);

                    xform.WorldRotation = physicsComponent.LinearVelocity.GetDir().ToAngle();
                    return;
                }
            }

            // Regular movement.
            // Target velocity.
            // This is relative to the map / grid we're on.
            var total = walkDir * mover.CurrentWalkSpeed + sprintDir * mover.CurrentSprintSpeed;

            var parentRotation = GetParentGridAngle(xform, mover);

            var worldTotal = _relativeMovement ? parentRotation.RotateVec(total) : total;

            DebugTools.Assert(MathHelper.CloseToPercent(total.Length, worldTotal.Length));

            if (weightless)
                worldTotal *= mobMover.WeightlessStrength;

            if (xform.GridID != GridId.Invalid)
                mover.LastGridAngle = parentRotation;

            if (worldTotal != Vector2.Zero)
            {
                // This should have its event run during island solver soooo
                xform.DeferUpdates = true;
                xform.WorldRotation = worldTotal.GetDir().ToAngle();
                xform.DeferUpdates = false;
                HandleFootsteps(mover, mobMover);
            }

            _physics.SetLinearVelocity(physicsComponent, worldTotal);
        }

        public bool UseMobMovement(EntityUid uid)
        {
            return UsedMobMovement.TryGetValue(uid, out var used) && used;
        }

        protected bool UseMobMovement(PhysicsComponent body)
        {
            return body.BodyStatus == BodyStatus.OnGround &&
                   EntityManager.HasComponent<MobStateComponent>(body.Owner) &&
                   // If we're being pulled then don't mess with our velocity.
                   (!EntityManager.TryGetComponent(body.Owner, out SharedPullableComponent? pullable) || !pullable.BeingPulled) &&
                   _blocker.CanMove((body).Owner);
        }

        /// <summary>
        ///     Used for weightlessness to determine if we are near a wall.
        /// </summary>
        public static bool IsAroundCollider(SharedPhysicsSystem broadPhaseSystem, TransformComponent transform, IMobMoverComponent mover, IPhysBody collider)
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
                    (IoCManager.Resolve<IEntityManager>().TryGetComponent(otherCollider.Owner, out SharedPullableComponent? pullable) && pullable.BeingPulled))
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
