using System.Diagnostics.CodeAnalysis;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Friction;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Pulling.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Player;
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
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly TagSystem _tags = default!;

        private const float StepSoundMoveDistanceRunning = 2;
        private const float StepSoundMoveDistanceWalking = 1.5f;

        private const float FootstepVariation = 0f;
        private const float FootstepVolume = 3f;
        private const float FootstepWalkingAddedVolumeMultiplier = 0f;

        /// <summary>
        /// <see cref="CCVars.MinimumFrictionSpeed"/>
        /// </summary>
        private float _minimumFrictionSpeed;

        /// <summary>
        /// <see cref="CCVars.StopSpeed"/>
        /// </summary>
        private float _stopSpeed;

        /// <summary>
        /// <see cref="CCVars.MobAcceleration"/>
        /// </summary>
        private float _mobAcceleration;

        /// <summary>
        /// <see cref="CCVars.MobFriction"/>
        /// </summary>
        private float _frictionVelocity;

        /// <summary>
        /// <see cref="CCVars.MobWeightlessAcceleration"/>
        /// </summary>
        private float _mobWeightlessAcceleration;

        /// <summary>
        /// <see cref="CCVars.MobWeightlessFriction"/>
        /// </summary>
        private float _weightlessFrictionVelocity;

        /// <summary>
        /// <see cref="CCVars.MobWeightlessFrictionNoInput"/>
        /// </summary>
        private float _weightlessFrictionVelocityNoInput;

        /// <summary>
        /// <see cref="CCVars.MobWeightlessModifier"/>
        /// </summary>
        private float _mobWeightlessModifier;

        private bool _relativeMovement;

        /// <summary>
        /// Cache the mob movement calculation to re-use elsewhere.
        /// </summary>
        public Dictionary<EntityUid, bool> UsedMobMovement = new();

        public override void Initialize()
        {
            base.Initialize();
            InitializeInput();
            InitializePushing();
            // Hello
            _configManager.OnValueChanged(CCVars.RelativeMovement, SetRelativeMovement, true);
            _configManager.OnValueChanged(CCVars.MinimumFrictionSpeed, SetMinimumFrictionSpeed, true);
            _configManager.OnValueChanged(CCVars.MobFriction, SetFrictionVelocity, true);
            _configManager.OnValueChanged(CCVars.MobWeightlessFriction, SetWeightlessFrictionVelocity, true);
            _configManager.OnValueChanged(CCVars.StopSpeed, SetStopSpeed, true);
            _configManager.OnValueChanged(CCVars.MobAcceleration, SetMobAcceleration, true);
            _configManager.OnValueChanged(CCVars.MobWeightlessAcceleration, SetMobWeightlessAcceleration, true);
            _configManager.OnValueChanged(CCVars.MobWeightlessFrictionNoInput, SetWeightlessFrictionNoInput, true);
            _configManager.OnValueChanged(CCVars.MobWeightlessModifier, SetMobWeightlessModifier, true);
            UpdatesBefore.Add(typeof(SharedTileFrictionController));
        }

        private void SetRelativeMovement(bool value) => _relativeMovement = value;
        private void SetMinimumFrictionSpeed(float value) => _minimumFrictionSpeed = value;
        private void SetStopSpeed(float value) => _stopSpeed = value;
        private void SetFrictionVelocity(float value) => _frictionVelocity = value;
        private void SetWeightlessFrictionVelocity(float value) => _weightlessFrictionVelocity = value;
        private void SetMobAcceleration(float value) => _mobAcceleration = value;
        private void SetMobWeightlessAcceleration(float value) => _mobWeightlessAcceleration = value;
        private void SetWeightlessFrictionNoInput(float value) => _weightlessFrictionVelocityNoInput = value;
        private void SetMobWeightlessModifier(float value) => _mobWeightlessModifier = value;

        public override void Shutdown()
        {
            base.Shutdown();
            ShutdownInput();
            ShutdownPushing();
            _configManager.UnsubValueChanged(CCVars.RelativeMovement, SetRelativeMovement);
            _configManager.UnsubValueChanged(CCVars.MinimumFrictionSpeed, SetMinimumFrictionSpeed);
            _configManager.UnsubValueChanged(CCVars.StopSpeed, SetStopSpeed);
            _configManager.UnsubValueChanged(CCVars.MobFriction, SetFrictionVelocity);
            _configManager.UnsubValueChanged(CCVars.MobWeightlessFriction, SetWeightlessFrictionVelocity);
            _configManager.UnsubValueChanged(CCVars.MobAcceleration, SetMobAcceleration);
            _configManager.UnsubValueChanged(CCVars.MobWeightlessAcceleration, SetMobWeightlessAcceleration);
            _configManager.UnsubValueChanged(CCVars.MobWeightlessFrictionNoInput, SetWeightlessFrictionNoInput);
            _configManager.UnsubValueChanged(CCVars.MobWeightlessModifier, SetMobWeightlessModifier);
        }

        public override void UpdateAfterSolve(bool prediction, float frameTime)
        {
            base.UpdateAfterSolve(prediction, frameTime);
            UsedMobMovement.Clear();
        }

        protected Angle GetParentGridAngle(TransformComponent xform, IMoverComponent mover)
        {
            if (!_mapManager.TryGetGrid(xform.GridUid, out var grid))
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
            var moveSpeedComponent = CompOrNull<MovementSpeedModifierComponent>(mover.Owner);
            var walkSpeed = moveSpeedComponent?.CurrentWalkSpeed ?? MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
            var sprintSpeed = moveSpeedComponent?.CurrentSprintSpeed ?? MovementSpeedModifierComponent.DefaultBaseSprintSpeed;
            var total = walkDir * walkSpeed + sprintDir * sprintSpeed;

            var worldTotal = _relativeMovement ? parentRotation.RotateVec(total) : total;

            if (transform.GridUid != null)
                mover.LastGridAngle = parentRotation;

            if (worldTotal != Vector2.Zero)
                transform.LocalRotation = transform.GridUid != null
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
            var weightless = mover.Owner.IsWeightless(physicsComponent, mapManager: _mapManager, entityManager: EntityManager);
            var (walkDir, sprintDir) = mover.VelocityDir;
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
                    touching = ev.CanMove || IsAroundCollider(_physics, xform, mobMover, physicsComponent);
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
                    friction = _weightlessFrictionVelocity;
                else
                    friction = _weightlessFrictionVelocityNoInput;

                weightlessModifier = _mobWeightlessModifier;
                accel = _mobWeightlessAcceleration;
            }
            else
            {
                friction = _frictionVelocity;
                weightlessModifier = 1f;
                accel = _mobAcceleration;
            }

            var profile = new MobMovementProfileEvent(
                touching,
                weightless,
                friction,
                weightlessModifier,
                accel);

            RaiseLocalEvent(xform.Owner, ref profile);

            if (profile.Override)
            {
                friction = profile.Friction;
                weightlessModifier = profile.WeightlessModifier;
                accel = profile.Acceleration;
            }

            Friction(frameTime, friction, ref velocity);

            if (xform.GridUid != EntityUid.Invalid)
                mover.LastGridAngle = parentRotation;

            if (worldTotal != Vector2.Zero)
            {
                // This should have its event run during island solver soooo
                xform.DeferUpdates = true;
                xform.LocalRotation = xform.GridUid != null
                    ? total.ToWorldAngle()
                    : worldTotal.ToWorldAngle();
                xform.DeferUpdates = false;

                if (!weightless && TryGetSound(mover, mobMover, xform, out var variation, out var sound))
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

            _physics.SetLinearVelocity(physicsComponent, velocity);
        }

        private void Friction(float frameTime, float friction, ref Vector2 velocity)
        {
            var speed = velocity.Length;

            if (speed < _minimumFrictionSpeed) return;

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
