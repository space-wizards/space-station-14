using System.Diagnostics.CodeAnalysis;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Friction;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Pulling.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Movement.Systems
{
    /// <summary>
    ///     Handles player and NPC mob movement.
    ///     NPCs are handled server-side only.
    /// </summary>
    public abstract partial class SharedMoverController : VirtualController
    {
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] protected readonly IGameTiming Timing = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedGravitySystem _gravity = default!;
        [Dependency] private readonly SharedMobStateSystem _mobState = default!;
        [Dependency] private readonly TagSystem _tags = default!;

        private const float StepSoundMoveDistanceRunning = 2;
        private const float StepSoundMoveDistanceWalking = 1.5f;

        private const float FootstepVariation = 0f;
        private const float FootstepVolume = 3f;
        private const float FootstepWalkingAddedVolumeMultiplier = 0f;

        /// <summary>
        /// <see cref="CCVars.StopSpeed"/>
        /// </summary>
        private float _stopSpeed;

        private bool _relativeMovement;

        /// <summary>
        /// Cache the mob movement calculation to re-use elsewhere.
        /// </summary>
        public Dictionary<EntityUid, bool> UsedMobMovement = new();

        public override void Initialize()
        {
            base.Initialize();
            InitializeInput();
            InitializeMob();
            InitializePushing();
            InitializeRelay();
            _configManager.OnValueChanged(CCVars.RelativeMovement, SetRelativeMovement, true);
            _configManager.OnValueChanged(CCVars.StopSpeed, SetStopSpeed, true);
            UpdatesBefore.Add(typeof(SharedTileFrictionController));
        }

        private void SetRelativeMovement(bool value) => _relativeMovement = value;
        private void SetStopSpeed(float value) => _stopSpeed = value;

        public override void Shutdown()
        {
            base.Shutdown();
            ShutdownInput();
            ShutdownPushing();
            _configManager.UnsubValueChanged(CCVars.RelativeMovement, SetRelativeMovement);
            _configManager.UnsubValueChanged(CCVars.StopSpeed, SetStopSpeed);
        }

        public override void UpdateAfterSolve(bool prediction, float frameTime)
        {
            base.UpdateAfterSolve(prediction, frameTime);
            UsedMobMovement.Clear();
        }

        protected Angle GetParentGridAngle(TransformComponent xform, InputMoverComponent mover)
        {
            if (!_mapManager.TryGetGrid(xform.GridUid, out var grid))
                return mover.LastGridAngle;

            return grid.WorldRotation;
        }

        /// <summary>
        ///     Movement while considering actionblockers, weightlessness, etc.
        /// </summary>
        protected void HandleMobMovement(
            InputMoverComponent mover,
            PhysicsComponent physicsComponent,
            TransformComponent xform,
            float frameTime)
        {
            DebugTools.Assert(!UsedMobMovement.ContainsKey(mover.Owner));

            if (!UseMobMovement(mover, physicsComponent))
            {
                UsedMobMovement[mover.Owner] = false;
                return;
            }

            UsedMobMovement[mover.Owner] = true;
            var weightless = _gravity.IsWeightless(mover.Owner, physicsComponent, xform);
            var (walkDir, sprintDir) = GetVelocityInput(mover);
            var touching = false;

            // Handle wall-pushes.
            if (weightless)
            {
                if (xform.GridUid != null)
                    touching = true;

                if (!touching)
                {
                    var ev = new CanWeightlessMoveEvent();
                    RaiseLocalEvent(xform.Owner, ref ev);
                    // No gravity: is our entity touching anything?
                    touching = ev.CanMove;

                    if (!touching && TryComp<MobMoverComponent>(xform.Owner, out var mobMover))
                        touching |= IsAroundCollider(PhysicsSystem, xform, mobMover, physicsComponent);
                }

                if (!touching)
                {
                    if (xform.GridUid != null)
                        mover.LastGridAngle = GetParentGridAngle(xform, mover);
                }
            }

            // Regular movement.
            // Target velocity.
            // This is relative to the map / grid we're on.
            var moveSpeedComponent = CompOrNull<MovementSpeedModifierComponent>(mover.Owner);

            var walkSpeed = moveSpeedComponent?.CurrentWalkSpeed ?? MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
            var sprintSpeed = moveSpeedComponent?.CurrentSprintSpeed ?? MovementSpeedModifierComponent.DefaultBaseSprintSpeed;

            var total = walkDir * walkSpeed + sprintDir * sprintSpeed;

            var parentRotation = GetParentGridAngle(xform, mover);
            var worldTotal = _relativeMovement ? parentRotation.RotateVec(total) : total;

            DebugTools.Assert(MathHelper.CloseToPercent(total.Length, worldTotal.Length));

            var velocity = physicsComponent.LinearVelocity;
            float friction;
            float weightlessModifier;
            float accel;

            if (weightless)
            {
                if (worldTotal != Vector2.Zero && touching)
                    friction = moveSpeedComponent?.WeightlessFriction ?? MovementSpeedModifierComponent.DefaultWeightlessFriction;
                else
                    friction = moveSpeedComponent?.WeightlessFrictionNoInput ?? MovementSpeedModifierComponent.DefaultWeightlessFrictionNoInput;

                weightlessModifier = moveSpeedComponent?.WeightlessModifier ?? MovementSpeedModifierComponent.DefaultWeightlessModifier;
                accel = moveSpeedComponent?.WeightlessAcceleration ?? MovementSpeedModifierComponent.DefaultWeightlessAcceleration;
            }
            else
            {
                if (worldTotal != Vector2.Zero || moveSpeedComponent?.FrictionNoInput == null)
                {
                    friction = moveSpeedComponent?.Friction ?? MovementSpeedModifierComponent.DefaultFriction;
                }
                else
                {
                    friction = moveSpeedComponent.FrictionNoInput ?? MovementSpeedModifierComponent.DefaultFrictionNoInput;
                }

                weightlessModifier = 1f;
                accel = moveSpeedComponent?.Acceleration ?? MovementSpeedModifierComponent.DefaultAcceleration;
            }

            var minimumFrictionSpeed = moveSpeedComponent?.MinimumFrictionSpeed ?? MovementSpeedModifierComponent.DefaultMinimumFrictionSpeed;
            Friction(minimumFrictionSpeed, frameTime, friction, ref velocity);

            if (xform.GridUid != EntityUid.Invalid)
                mover.LastGridAngle = parentRotation;

            if (worldTotal != Vector2.Zero)
            {
                // This should have its event run during island solver soooo
                xform.DeferUpdates = true;
                TransformComponent rotateXform;

                // If we're in a container then relay rotation to the parent instead
                if (_container.TryGetContainingContainer(xform.Owner, out var container))
                {
                    rotateXform = Transform(container.Owner);
                }
                else
                {
                    rotateXform = xform;
                }

                rotateXform.LocalRotation = xform.GridUid != null
                    ? total.ToWorldAngle()
                    : worldTotal.ToWorldAngle();
                rotateXform.DeferUpdates = false;

                if (!weightless && TryComp<MobMoverComponent>(mover.Owner, out var mobMover) && TryGetSound(mover, mobMover, xform, out var variation, out var sound))
                {
                    var soundModifier = mover.Sprinting ? 1.0f : FootstepWalkingAddedVolumeMultiplier;
                    SoundSystem.Play(sound,
                        GetSoundPlayers(mover.Owner),
                        mover.Owner, AudioHelpers.WithVariation(variation).WithVolume(FootstepVolume * soundModifier));
                }
            }

            worldTotal *= weightlessModifier;

            if (!weightless || touching)
                Accelerate(ref velocity, in worldTotal, accel, frameTime);

            PhysicsSystem.SetLinearVelocity(physicsComponent, velocity);
        }

        private void Friction(float minimumFrictionSpeed, float frameTime, float friction, ref Vector2 velocity)
        {
            var speed = velocity.Length;

            if (speed < minimumFrictionSpeed) return;

            var drop = 0f;

            var control = MathF.Max(_stopSpeed, speed);
            drop += control * friction * frameTime;

            var newSpeed = MathF.Max(0f, speed - drop);

            if (newSpeed.Equals(speed)) return;

            newSpeed /= speed;
            velocity *= newSpeed;
        }

        private void Accelerate(ref Vector2 currentVelocity, in Vector2 velocity, float accel, float frameTime)
        {
            var wishDir = velocity != Vector2.Zero ? velocity.Normalized : Vector2.Zero;
            var wishSpeed = velocity.Length;

            var currentSpeed = Vector2.Dot(currentVelocity, wishDir);
            var addSpeed = wishSpeed - currentSpeed;

            if (addSpeed <= 0f) return;

            var accelSpeed = accel * frameTime * wishSpeed;
            accelSpeed = MathF.Min(accelSpeed, addSpeed);

            currentVelocity += wishDir * accelSpeed;
        }

        public bool UseMobMovement(EntityUid uid)
        {
            return UsedMobMovement.TryGetValue(uid, out var used) && used;
        }

        protected bool UseMobMovement(InputMoverComponent mover, PhysicsComponent body)
        {
            return mover.CanMove &&
                   body.BodyStatus == BodyStatus.OnGround &&
                   HasComp<InputMoverComponent>(body.Owner) &&
                   // If we're being pulled then don't mess with our velocity.
                   (!TryComp(body.Owner, out SharedPullableComponent? pullable) || !pullable.BeingPulled);
        }

        /// <summary>
        ///     Used for weightlessness to determine if we are near a wall.
        /// </summary>
        private bool IsAroundCollider(SharedPhysicsSystem broadPhaseSystem, TransformComponent transform, MobMoverComponent mover, IPhysBody collider)
        {
            var enlargedAABB = collider.GetWorldAABB().Enlarged(mover.GrabRangeVV);

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

        private bool TryGetSound(InputMoverComponent mover, MobMoverComponent mobMover, TransformComponent xform, out float variation, [NotNullWhen(true)] out string? sound)
        {
            sound = null;
            variation = 0f;

            if (!CanSound() || !_tags.HasTag(mover.Owner, "FootstepSound")) return false;

            var coordinates = xform.Coordinates;
            var gridId = coordinates.GetGridUid(EntityManager);
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

            DebugTools.Assert(gridId != null);
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

            return TryGetFootstepSound(gridId!.Value, coordinates, out variation, out sound);
        }

        private bool TryGetFootstepSound(EntityUid gridId, EntityCoordinates coordinates, out float variation, [NotNullWhen(true)] out string? sound)
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
