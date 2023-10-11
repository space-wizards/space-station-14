using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Bed.Sleep;
using Content.Shared.CCVar;
using Content.Shared.Friction;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Pulling.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Systems;
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
        [Dependency] private   readonly IConfigurationManager _configManager = default!;
        [Dependency] protected readonly IGameTiming Timing = default!;
        [Dependency] private   readonly IMapManager _mapManager = default!;
        [Dependency] private   readonly ITileDefinitionManager _tileDefinitionManager = default!;
        [Dependency] private   readonly EntityLookupSystem _lookup = default!;
        [Dependency] private   readonly InventorySystem _inventory = default!;
        [Dependency] private   readonly MobStateSystem _mobState = default!;
        [Dependency] private   readonly SharedAudioSystem _audio = default!;
        [Dependency] private   readonly SharedContainerSystem _container = default!;
        [Dependency] private   readonly SharedMapSystem _mapSystem = default!;
        [Dependency] private   readonly SharedGravitySystem _gravity = default!;
        [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
        [Dependency] private   readonly SharedTransformSystem _transform = default!;
        [Dependency] private   readonly TagSystem _tags = default!;

        protected EntityQuery<InputMoverComponent> MoverQuery;
        protected EntityQuery<MobMoverComponent> MobMoverQuery;
        protected EntityQuery<MovementRelayTargetComponent> RelayTargetQuery;
        protected EntityQuery<MovementSpeedModifierComponent> ModifierQuery;
        protected EntityQuery<PhysicsComponent> PhysicsQuery;
        protected EntityQuery<RelayInputMoverComponent> RelayQuery;
        protected EntityQuery<SharedPullableComponent> PullableQuery;
        protected EntityQuery<TransformComponent> XformQuery;
        protected EntityQuery<CanMoveInAirComponent> CanMoveInAirQuery;
        protected EntityQuery<NoRotateOnMoveComponent> NoRotateQuery;

        private const float StepSoundMoveDistanceRunning = 2;
        private const float StepSoundMoveDistanceWalking = 1.5f;

        private const float FootstepVariation = 0f;

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

            MoverQuery = GetEntityQuery<InputMoverComponent>();
            MobMoverQuery = GetEntityQuery<MobMoverComponent>();
            ModifierQuery = GetEntityQuery<MovementSpeedModifierComponent>();
            RelayTargetQuery = GetEntityQuery<MovementRelayTargetComponent>();
            PhysicsQuery = GetEntityQuery<PhysicsComponent>();
            RelayQuery = GetEntityQuery<RelayInputMoverComponent>();
            PullableQuery = GetEntityQuery<SharedPullableComponent>();
            XformQuery = GetEntityQuery<TransformComponent>();
            NoRotateQuery = GetEntityQuery<NoRotateOnMoveComponent>();
            CanMoveInAirQuery = GetEntityQuery<CanMoveInAirComponent>();

            InitializeInput();
            InitializeRelay();
            _configManager.OnValueChanged(CCVars.RelativeMovement, SetRelativeMovement, true);
            _configManager.OnValueChanged(CCVars.StopSpeed, SetStopSpeed, true);
            UpdatesBefore.Add(typeof(TileFrictionController));
        }

        private void SetRelativeMovement(bool value) => _relativeMovement = value;
        private void SetStopSpeed(float value) => _stopSpeed = value;

        public override void Shutdown()
        {
            base.Shutdown();
            ShutdownInput();
            _configManager.UnsubValueChanged(CCVars.RelativeMovement, SetRelativeMovement);
            _configManager.UnsubValueChanged(CCVars.StopSpeed, SetStopSpeed);
        }

        public override void UpdateAfterSolve(bool prediction, float frameTime)
        {
            base.UpdateAfterSolve(prediction, frameTime);
            UsedMobMovement.Clear();
        }

        /// <summary>
        ///     Movement while considering actionblockers, weightlessness, etc.
        /// </summary>
        protected void HandleMobMovement(
            EntityUid uid,
            InputMoverComponent mover,
            EntityUid physicsUid,
            PhysicsComponent physicsComponent,
            TransformComponent xform,
            float frameTime)
        {
            var canMove = mover.CanMove;
            if (RelayTargetQuery.TryGetComponent(uid, out var relayTarget))
            {
                if (_mobState.IsIncapacitated(relayTarget.Source) ||
                    TryComp<SleepingComponent>(relayTarget.Source, out _) ||
                    !MoverQuery.TryGetComponent(relayTarget.Source, out var relayedMover))
                {
                    canMove = false;
                }
                else
                {
                    mover.RelativeEntity = relayedMover.RelativeEntity;
                    mover.RelativeRotation = relayedMover.RelativeRotation;
                    mover.TargetRelativeRotation = relayedMover.TargetRelativeRotation;
                }
            }

            // Update relative movement
            if (mover.LerpTarget < Timing.CurTime)
            {
                if (TryUpdateRelative(mover, xform))
                {
                    Dirty(uid, mover);
                }
            }

            LerpRotation(uid, mover, frameTime);

            if (!canMove
                || physicsComponent.BodyStatus != BodyStatus.OnGround && !CanMoveInAirQuery.HasComponent(uid)
                || PullableQuery.TryGetComponent(uid, out var pullable) && pullable.BeingPulled)
            {
                UsedMobMovement[uid] = false;
                return;
            }


            UsedMobMovement[uid] = true;
            // Specifically don't use mover.Owner because that may be different to the actual physics body being moved.
            var weightless = _gravity.IsWeightless(physicsUid, physicsComponent, xform);
            var (walkDir, sprintDir) = GetVelocityInput(mover);
            var touching = false;

            // Handle wall-pushes.
            if (weightless)
            {
                if (xform.GridUid != null)
                    touching = true;

                if (!touching)
                {
                    var ev = new CanWeightlessMoveEvent(uid);
                    RaiseLocalEvent(uid, ref ev, true);
                    // No gravity: is our entity touching anything?
                    touching = ev.CanMove;

                    if (!touching && TryComp<MobMoverComponent>(uid, out var mobMover))
                        touching |= IsAroundCollider(PhysicsSystem, xform, mobMover, physicsUid, physicsComponent);
                }
            }

            // Get current tile def for things like speed/friction mods
            ContentTileDefinition? tileDef = null;

            // Don't bother getting the tiledef here if we're weightless or in-air
            // since no tile-based modifiers should be applying in that situation
            if (_mapManager.TryFindGridAt(xform.MapPosition, out var grid, out var gridComp)
                && _mapSystem.TryGetTileRef(grid, gridComp, xform.Coordinates, out var tile)
                && !(weightless || physicsComponent.BodyStatus == BodyStatus.InAir))
            {
                tileDef = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];
            }

            // Regular movement.
            // Target velocity.
            // This is relative to the map / grid we're on.
            var moveSpeedComponent = ModifierQuery.CompOrNull(uid);

            var walkSpeed = moveSpeedComponent?.CurrentWalkSpeed ?? MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
            var sprintSpeed = moveSpeedComponent?.CurrentSprintSpeed ?? MovementSpeedModifierComponent.DefaultBaseSprintSpeed;

            var total = walkDir * walkSpeed + sprintDir * sprintSpeed;

            var parentRotation = GetParentGridAngle(mover);
            var worldTotal = _relativeMovement ? parentRotation.RotateVec(total) : total;

            DebugTools.Assert(MathHelper.CloseToPercent(total.Length(), worldTotal.Length()));

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
                    friction = tileDef?.MobFriction ?? moveSpeedComponent?.Friction ?? MovementSpeedModifierComponent.DefaultFriction;
                }
                else
                {
                    friction = tileDef?.MobFrictionNoInput ?? moveSpeedComponent.FrictionNoInput ?? MovementSpeedModifierComponent.DefaultFrictionNoInput;
                }

                weightlessModifier = 1f;
                accel = tileDef?.MobAcceleration ?? moveSpeedComponent?.Acceleration ?? MovementSpeedModifierComponent.DefaultAcceleration;
            }

            var minimumFrictionSpeed = moveSpeedComponent?.MinimumFrictionSpeed ?? MovementSpeedModifierComponent.DefaultMinimumFrictionSpeed;
            Friction(minimumFrictionSpeed, frameTime, friction, ref velocity);

            if (worldTotal != Vector2.Zero)
            {
                if (!NoRotateQuery.HasComponent(uid))
                {
                    // TODO apparently this results in a duplicate move event because "This should have its event run during
                    // island solver"??. So maybe SetRotation needs an argument to avoid raising an event?
                    var worldRot = _transform.GetWorldRotation(xform);
                    _transform.SetLocalRotation(xform, xform.LocalRotation + worldTotal.ToWorldAngle() - worldRot);
                }

                if (!weightless && MobMoverQuery.TryGetComponent(uid, out var mobMover) &&
                    TryGetSound(weightless, uid, mover, mobMover, xform, out var sound, tileDef: tileDef))
                {
                    var soundModifier = mover.Sprinting ? 3.5f : 1.5f;

                    var audioParams = sound.Params
                        .WithVolume(sound.Params.Volume + soundModifier)
                        .WithVariation(sound.Params.Variation ?? FootstepVariation);

                    // If we're a relay target then predict the sound for all relays.
                    if (relayTarget != null)
                    {
                        _audio.PlayPredicted(sound, uid, relayTarget.Source, audioParams);
                    }
                    else
                    {
                        _audio.PlayPredicted(sound, uid, uid, audioParams);
                    }
                }
            }

            worldTotal *= weightlessModifier;

            if (!weightless || touching)
                Accelerate(ref velocity, in worldTotal, accel, frameTime);

            PhysicsSystem.SetLinearVelocity(physicsUid, velocity, body: physicsComponent);

            // Ensures that players do not spiiiiiiin
            PhysicsSystem.SetAngularVelocity(physicsUid, 0, body: physicsComponent);
        }

        public void LerpRotation(EntityUid uid, InputMoverComponent mover, float frameTime)
        {
            var angleDiff = Angle.ShortestDistance(mover.RelativeRotation, mover.TargetRelativeRotation);

            // if we've just traversed then lerp to our target rotation.
            if (!angleDiff.EqualsApprox(Angle.Zero, 0.001))
            {
                var adjustment = angleDiff * 5f * frameTime;
                var minAdjustment = 0.01 * frameTime;

                if (angleDiff < 0)
                {
                    adjustment = Math.Min(adjustment, -minAdjustment);
                    adjustment = Math.Clamp(adjustment, angleDiff, -angleDiff);
                }
                else
                {
                    adjustment = Math.Max(adjustment, minAdjustment);
                    adjustment = Math.Clamp(adjustment, -angleDiff, angleDiff);
                }

                mover.RelativeRotation += adjustment;
                mover.RelativeRotation.FlipPositive();
                Dirty(uid, mover);
            }
            else if (!angleDiff.Equals(Angle.Zero))
            {
                mover.TargetRelativeRotation.FlipPositive();
                mover.RelativeRotation = mover.TargetRelativeRotation;
                Dirty(uid, mover);
            }
        }

        private void Friction(float minimumFrictionSpeed, float frameTime, float friction, ref Vector2 velocity)
        {
            var speed = velocity.Length();

            if (speed < minimumFrictionSpeed)
                return;

            var drop = 0f;

            var control = MathF.Max(_stopSpeed, speed);
            drop += control * friction * frameTime;

            var newSpeed = MathF.Max(0f, speed - drop);

            if (newSpeed.Equals(speed))
                return;

            newSpeed /= speed;
            velocity *= newSpeed;
        }

        private void Accelerate(ref Vector2 currentVelocity, in Vector2 velocity, float accel, float frameTime)
        {
            var wishDir = velocity != Vector2.Zero ? velocity.Normalized() : Vector2.Zero;
            var wishSpeed = velocity.Length();

            var currentSpeed = Vector2.Dot(currentVelocity, wishDir);
            var addSpeed = wishSpeed - currentSpeed;

            if (addSpeed <= 0f)
                return;

            var accelSpeed = accel * frameTime * wishSpeed;
            accelSpeed = MathF.Min(accelSpeed, addSpeed);

            currentVelocity += wishDir * accelSpeed;
        }

        public bool UseMobMovement(EntityUid uid)
        {
            return UsedMobMovement.TryGetValue(uid, out var used) && used;
        }

        /// <summary>
        ///     Used for weightlessness to determine if we are near a wall.
        /// </summary>
        private bool IsAroundCollider(SharedPhysicsSystem broadPhaseSystem, TransformComponent transform, MobMoverComponent mover, EntityUid physicsUid, PhysicsComponent collider)
        {
            var enlargedAABB = _lookup.GetWorldAABB(physicsUid, transform).Enlarged(mover.GrabRangeVV);

            foreach (var otherCollider in broadPhaseSystem.GetCollidingEntities(transform.MapID, enlargedAABB))
            {
                if (otherCollider == collider)
                    continue; // Don't try to push off of yourself!

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

        protected abstract bool CanSound();

        private bool TryGetSound(
            bool weightless,
            EntityUid uid,
            InputMoverComponent mover,
            MobMoverComponent mobMover,
            TransformComponent xform,
            [NotNullWhen(true)] out SoundSpecifier? sound,
            ContentTileDefinition? tileDef = null)
        {
            sound = null;

            if (!CanSound() || !_tags.HasTag(uid, "FootstepSound"))
                return false;

            var coordinates = xform.Coordinates;
            var distanceNeeded = mover.Sprinting ? StepSoundMoveDistanceRunning : StepSoundMoveDistanceWalking;

            // Handle footsteps.
            if (!weightless)
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

            mobMover.LastPosition = coordinates;

            if (mobMover.StepSoundDistance < distanceNeeded)
                return false;

            mobMover.StepSoundDistance -= distanceNeeded;

            if (TryComp<FootstepModifierComponent>(uid, out var moverModifier))
            {
                sound = moverModifier.FootstepSoundCollection;
                return true;
            }

            if (_inventory.TryGetSlotEntity(uid, "shoes", out var shoes) &&
                TryComp<FootstepModifierComponent>(shoes, out var modifier))
            {
                sound = modifier.FootstepSoundCollection;
                return true;
            }

            return TryGetFootstepSound(uid, xform, shoes != null, out sound, tileDef: tileDef);
        }

        private bool TryGetFootstepSound(
            EntityUid uid,
            TransformComponent xform,
            bool haveShoes,
            [NotNullWhen(true)] out SoundSpecifier? sound,
            ContentTileDefinition? tileDef = null)
        {
            sound = null;

            // Fallback to the map?
            if (!_mapManager.TryGetGrid(xform.GridUid, out var grid))
            {
                if (TryComp<FootstepModifierComponent>(xform.MapUid, out var modifier))
                {
                    sound = modifier.FootstepSoundCollection;
                    return true;
                }

                return false;
            }

            var position = grid.LocalToTile(xform.Coordinates);
            var soundEv = new GetFootstepSoundEvent(uid);

            // If the coordinates have a FootstepModifier component
            // i.e. component that emit sound on footsteps emit that sound
            var anchored = grid.GetAnchoredEntitiesEnumerator(position);

            while (anchored.MoveNext(out var maybeFootstep))
            {
                RaiseLocalEvent(maybeFootstep.Value, ref soundEv);

                if (soundEv.Sound != null)
                {
                    sound = soundEv.Sound;
                    return true;
                }

                if (TryComp<FootstepModifierComponent>(maybeFootstep, out var footstep))
                {
                    sound = footstep.FootstepSoundCollection;
                    return true;
                }
            }

            // Walking on a tile.
            // Tile def might have been passed in already from previous methods, so use that
            // if we have it
            if (tileDef == null && grid.TryGetTileRef(position, out var tileRef))
            {
                tileDef = (ContentTileDefinition) _tileDefinitionManager[tileRef.Tile.TypeId];
            }

            if (tileDef == null)
                return false;

            sound = haveShoes ? tileDef.FootstepSounds : tileDef.BarestepSounds;
            return sound != null;
        }
    }
}
