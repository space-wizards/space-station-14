using System.Diagnostics.CodeAnalysis;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Friction;
using Content.Shared.Maps;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Pulling.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    private const float StepSoundMoveDistanceRunning = 2;
    private const float StepSoundMoveDistanceWalking = 1.5f;

    private const float FootstepVariation = 0f;
    private const float FootstepVolume = 1f;

    /// <summary>
    /// <see cref="CCVars.StopSpeed"/>
    /// </summary>
    private float _stopSpeed;

    /// <summary>
    /// <see cref="CCVars.RelativeMovement"/>
    /// </summary>
    private bool _relativeMovement;

    /// <summary>
    /// Cache the mob movement calculation to re-use elsewhere.
    /// </summary>
    public Dictionary<EntityUid, bool> UsedMobMovement = new();

    private void InitializeMobMovement()
    {
        SubscribeLocalEvent<MobMoverComponent, ComponentGetState>(OnMobGetState);
        SubscribeLocalEvent<MobMoverComponent, ComponentHandleState>(OnMobHandleState);
        SubscribeLocalEvent<MobMoverComponent, ComponentInit>(OnMobInit);

        _configManager.OnValueChanged(CCVars.RelativeMovement, SetRelativeMovement, true);
        _configManager.OnValueChanged(CCVars.StopSpeed, SetStopSpeed, true);
        UpdatesBefore.Add(typeof(SharedTileFrictionController));
    }

    private void SetRelativeMovement(bool value) => _relativeMovement = value;
    private void SetStopSpeed(float value) => _stopSpeed = value;

    private void ShutdownMobMovement()
    {
        _configManager.UnsubValueChanged(CCVars.RelativeMovement, SetRelativeMovement);
    }

    public override void UpdateAfterSolve(bool prediction, float frameTime)
    {
        base.UpdateAfterSolve(prediction, frameTime);
        UsedMobMovement.Clear();
    }

    private void OnMobInit(EntityUid uid, MobMoverComponent component, ComponentInit args)
    {
        // TODO: AAA
        component.LastGridAngle = Transform(uid).Parent?.WorldRotation ?? new Angle(0);
    }

    private void OnMobHandleState(EntityUid uid, MobMoverComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MobMoverComponentState state) return;
        component.GrabRange = state.GrabRange;
        component.PushStrength = state.PushStrength;
        component.BaseWalkSpeed = state.BaseWalkSpeed;
        component.BaseSprintSpeed = state.BaseSprintSpeed;
        component.WalkSpeedModifier = state.WalkSpeedModifier;
        component.SprintSpeedModifier = state.SprintSpeedModifier;
        component.HeldMoveButtons = state.Buttons;
        component.LastInputTick = GameTick.Zero;
        component.LastInputSubTick = 0;
        component.CanMove = state.CanMove;
    }

    private void OnMobGetState(EntityUid uid, MobMoverComponent component, ref ComponentGetState args)
    {
        args.State = new MobMoverComponentState(
            component.GrabRange,
            component.PushStrength,
            component.BaseWalkSpeed,
            component.BaseSprintSpeed,
            component.WalkSpeedModifier,
            component.SprintSpeedModifier,
            component.CanMove,
            component.HeldMoveButtons);
    }

    public void RefreshMovementSpeedModifiers(EntityUid uid, MobMoverComponent? move = null)
    {
        if (!Resolve(uid, ref move, false))
            return;

        var ev = new RefreshMovementSpeedModifiersEvent();
        RaiseLocalEvent(uid, ev);

        if (move.WalkSpeedModifier.Equals(ev.WalkSpeedModifier) &&
            move.SprintSpeedModifier.Equals(ev.SprintSpeedModifier)) return;

        move.WalkSpeedModifier = ev.WalkSpeedModifier;
        move.SprintSpeedModifier = ev.SprintSpeedModifier;

        Dirty(move);
    }

    protected Angle GetParentGridAngle(TransformComponent xform, MobMoverComponent mover)
    {
        if (!_mapManager.TryGetGrid(xform.GridUid, out var grid))
            return mover.LastGridAngle;

        return grid.WorldRotation;
    }

        /// <summary>
        ///     Movement while considering actionblockers, weightlessness, etc.
        /// </summary>
    protected void HandleMobMovement(
        MobMoverComponent mover,
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
        var weightless = mover.Owner.IsWeightless(physicsComponent, mapManager: _mapManager, entityManager: EntityManager);
        var (walkDir, sprintDir) = GetMobVelocityInput(mover);
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
                touching = ev.CanMove || IsAroundCollider(_physics, xform, mover, physicsComponent);
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
        var walkSpeed = mover.CurrentWalkSpeed;
        var sprintSpeed = mover.CurrentSprintSpeed;
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
                friction = mover.WeightlessFrictionVelocity;
            else
                friction = mover.WeightlessFrictionVelocityNoInput;

            weightlessModifier = mover.WeightlessModifier;
            accel = mover.WeightlessAcceleration;
        }
        else
        {
            friction = mover.FrictionVelocity;
            weightlessModifier = 1f;
            accel = mover.Acceleration;
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

        Friction(mover, frameTime, friction, ref velocity);

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

            if (!weightless && TryGetSound(mover, xform, out var variation, out var sound))
            {
                SoundSystem.Play(sound,
                    GetSoundPlayers(mover.Owner),
                    mover.Owner, AudioHelpers.WithVariation(variation).WithVolume(FootstepVolume));
            }
        }

        worldTotal *= weightlessModifier;

        if (!weightless || touching)
            Accelerate(ref velocity, in worldTotal, accel, frameTime);

        _physics.SetLinearVelocity(physicsComponent, velocity);
    }

    private void Friction(MobMoverComponent mover, float frameTime, float friction, ref Vector2 velocity)
    {
        var speed = velocity.Length;

        if (speed < mover.MinimumFrictionSpeed) return;

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

    protected bool UseMobMovement(MobMoverComponent mover, PhysicsComponent body)
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
    private bool IsAroundCollider(SharedPhysicsSystem broadPhaseSystem, TransformComponent transform, MobMoverComponent mover, IPhysBody collider)
    {
        if (mover is not MobMoverComponent mobMover) return false;

        var enlargedAABB = collider.GetWorldAABB().Enlarged(mobMover.GrabRange);

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

    private bool TryGetSound(MobMoverComponent mover, TransformComponent xform, out float variation, [NotNullWhen(true)] out string? sound)
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
            if (!coordinates.TryDistance(EntityManager, mover.LastPosition, out var distance) ||
                distance > distanceNeeded)
            {
                mover.StepSoundDistance = distanceNeeded;
            }
            else
            {
                mover.StepSoundDistance += distance;
            }
        }
        else
        {
            // In space no one can hear you squeak
            return false;
        }

        DebugTools.Assert(gridId != null);
        mover.LastPosition = coordinates;

        if (mover.StepSoundDistance < distanceNeeded) return false;

        mover.StepSoundDistance -= distanceNeeded;

        if (_inventory.TryGetSlotEntity(mover.Owner, "shoes", out var shoes) &&
            TryComp<FootstepModifierComponent>(shoes, out var modifier))
        {
            sound = modifier.SoundCollection.GetSound(_random, _protoManager);
            variation = modifier.Variation;
            return true;
        }

        return TryGetFootstepSound(gridId!.Value, coordinates, out variation, out sound);
    }

    private bool TryGetFootstepSound(EntityUid gridId, EntityCoordinates coordinates, out float variation, [NotNullWhen(true)] out string? sound)
    {
        // TODO: Update footstep sounds to master.
        variation = 0f;
        sound = null;
        var grid = _mapManager.GetGrid(gridId);
        var tile = grid.GetTileRef(coordinates);

        if (tile.IsSpace(_tileDefinitionManager)) return false;

        // If the coordinates have a FootstepModifier component
        // i.e. component that emit sound on footsteps emit that sound
        foreach (var maybeFootstep in grid.GetAnchoredEntities(tile.GridIndices))
        {
            if (TryComp(maybeFootstep, out FootstepModifierComponent? footstep))
            {
                sound = footstep.SoundCollection.GetSound(_random, _protoManager);
                variation = footstep.Variation;
                return true;
            }
        }

        // Walking on a tile.
        var def = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];
        sound = def.FootstepSounds?.GetSound(_random, _protoManager);
        variation = FootstepVariation;

        return !string.IsNullOrEmpty(sound);
    }

    [Serializable, NetSerializable]
    protected sealed class MobMoverComponentState : ComponentState
    {
        public float GrabRange;
        public float PushStrength;
        public float BaseWalkSpeed;
        public float BaseSprintSpeed;
        public float WalkSpeedModifier;
        public float SprintSpeedModifier;
        public MoveButtons Buttons { get; }
        public readonly bool CanMove;

        public MobMoverComponentState(
            float grabRange,
            float pushStrength,
            float baseWalkSpeed,
            float baseSprintSpeed,
            float walkSpeedModifier,
            float sprintSpeedModifier,
            bool canMove,
            MoveButtons buttons)
        {
            GrabRange = grabRange;
            PushStrength = pushStrength;
            BaseWalkSpeed = baseWalkSpeed;
            BaseSprintSpeed = baseSprintSpeed;
            WalkSpeedModifier = walkSpeedModifier;
            SprintSpeedModifier = sprintSpeedModifier;
            CanMove = canMove;
            Buttons = buttons;
        }
    }
}
