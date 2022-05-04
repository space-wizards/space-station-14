using System.Diagnostics.CodeAnalysis;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Friction;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Pulling.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Player;
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
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly TagSystem _tags = default!;

        private const float StepSoundMoveDistanceRunning = 2;
        private const float StepSoundMoveDistanceWalking = 1.5f;

        private const float FootstepVariation = 0f;
        private const float FootstepVolume = 1f;

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
                transform.LocalRotation = transform.GridID != GridId.Invalid
                    ? total.ToWorldAngle()
                    : worldTotal.ToWorldAngle();

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

            if (!UseMobMovement(mover, physicsComponent))
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
                xform.LocalRotation = xform.GridID != GridId.Invalid
                    ? total.ToWorldAngle()
                    : worldTotal.ToWorldAngle();
                xform.DeferUpdates = false;

                if (TryGetSound(mover, mobMover, xform, out var variation, out var sound))
                {
                    SoundSystem.Play(
                        GetSoundPlayers(mover.Owner),
                        sound,
                        mover.Owner,
                        AudioHelpers.WithVariation(variation).WithVolume(FootstepVolume));
                }
            }

            _physics.SetLinearVelocity(physicsComponent, worldTotal);
        }

        public bool UseMobMovement(EntityUid uid)
        {
            return UsedMobMovement.TryGetValue(uid, out var used) && used;
        }

        protected bool UseMobMovement(IMoverComponent mover, PhysicsComponent body)
        {
            return mover.CanMove &&
                   body.BodyStatus == BodyStatus.OnGround &&
                   HasComp<MobStateComponent>(body.Owner) &&
                   // If we're being pulled then don't mess with our velocity.
                   (!TryComp(body.Owner, out SharedPullableComponent? pullable) || !pullable.BeingPulled);
        }

        /// <summary>
        ///     Used for weightlessness to determine if we are near a wall.
        /// </summary>
        private bool IsAroundCollider(SharedPhysicsSystem broadPhaseSystem, TransformComponent transform, IMobMoverComponent mover, IPhysBody collider)
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
                    (TryComp(otherCollider.Owner, out SharedPullableComponent? pullable) && pullable.BeingPulled))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        // TODO: Predicted audio moment.
        protected abstract Filter GetSoundPlayers(EntityUid mover);

        protected abstract bool CanSound();

        private bool TryGetSound(IMoverComponent mover, IMobMoverComponent mobMover, TransformComponent xform, out float variation, [NotNullWhen(true)] out string? sound)
        {
            sound = null;
            variation = 0f;

            if (!CanSound() || !_tags.HasTag(mover.Owner, "FootstepSound")) return false;

            var coordinates = xform.Coordinates;
            var gridId = coordinates.GetGridId(EntityManager);
            var distanceNeeded = mover.Sprinting ? StepSoundMoveDistanceRunning : StepSoundMoveDistanceWalking;

            // Handle footsteps.
            if (_mapManager.GridExists(gridId))
            {
                // Can happen when teleporting between grids.
                if (!coordinates.TryDistance(EntityManager, mobMover.LastPosition, out var distance) ||
                    distance > distanceNeeded)
                {
                    mobMover.StepSoundDistance = distanceNeeded;
                }
                else
                {
                    mobMover.StepSoundDistance += distance;
                }
            }
            else
            {
                // In space no one can hear you squeak
                return false;
            }

            DebugTools.Assert(gridId != GridId.Invalid);
            mobMover.LastPosition = coordinates;

            if (mobMover.StepSoundDistance < distanceNeeded) return false;

            mobMover.StepSoundDistance -= distanceNeeded;

            if (_inventory.TryGetSlotEntity(mover.Owner, "shoes", out var shoes) &&
                EntityManager.TryGetComponent<FootstepModifierComponent>(shoes, out var modifier))
            {
                sound = modifier.SoundCollection.GetSound();
                variation = modifier.Variation;
                return true;
            }

            return TryGetFootstepSound(gridId, coordinates, out variation, out sound);
        }

        private bool TryGetFootstepSound(GridId gridId, EntityCoordinates coordinates, out float variation, [NotNullWhen(true)] out string? sound)
        {
            variation = 0f;
            sound = null;
            var grid = _mapManager.GetGrid(gridId);
            var tile = grid.GetTileRef(coordinates);

            if (tile.IsSpace(_tileDefinitionManager)) return false;

            // If the coordinates have a FootstepModifier component
            // i.e. component that emit sound on footsteps emit that sound
            foreach (var maybeFootstep in grid.GetAnchoredEntities(tile.GridIndices))
            {
                if (EntityManager.TryGetComponent(maybeFootstep, out FootstepModifierComponent? footstep))
                {
                    sound = footstep.SoundCollection.GetSound();
                    variation = footstep.Variation;
                    return true;
                }
            }

            // Walking on a tile.
            var def = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];
            sound = def.FootstepSounds?.GetSound();
            variation = FootstepVariation;

            return !string.IsNullOrEmpty(sound);
        }
    }
}
