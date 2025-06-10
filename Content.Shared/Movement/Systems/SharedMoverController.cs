using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Bed.Sleep;
using Content.Shared.CCVar;
using Content.Shared.Friction;
using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using PullableComponent = Content.Shared.Movement.Pulling.Components.PullableComponent;

namespace Content.Shared.Movement.Systems;

/// <summary>
///     Handles player and NPC mob movement.
///     NPCs are handled server-side only.
/// </summary>
public abstract partial class SharedMoverController : VirtualController
{
    [Dependency] private   readonly IConfigurationManager _configManager = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private   readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private   readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private   readonly EntityLookupSystem _lookup = default!;
    [Dependency] private   readonly InventorySystem _inventory = default!;
    [Dependency] private   readonly MobStateSystem _mobState = default!;
    [Dependency] private   readonly SharedAudioSystem _audio = default!;
    [Dependency] private   readonly SharedContainerSystem _container = default!;
    [Dependency] private   readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private   readonly SharedGravitySystem _gravity = default!;
    [Dependency] private   readonly SharedTransformSystem _transform = default!;
    [Dependency] private   readonly TagSystem _tags = default!;

    protected EntityQuery<InputMoverComponent> MoverQuery;
    protected EntityQuery<MobMoverComponent> MobMoverQuery;
    protected EntityQuery<MovementRelayTargetComponent> RelayTargetQuery;
    protected EntityQuery<MovementSpeedModifierComponent> ModifierQuery;
    protected EntityQuery<PhysicsComponent> PhysicsQuery;
    protected EntityQuery<RelayInputMoverComponent> RelayQuery;
    protected EntityQuery<PullableComponent> PullableQuery;
    protected EntityQuery<TransformComponent> XformQuery;
    protected EntityQuery<CanMoveInAirComponent> CanMoveInAirQuery;
    protected EntityQuery<NoRotateOnMoveComponent> NoRotateQuery;
    protected EntityQuery<FootstepModifierComponent> FootstepModifierQuery;
    protected EntityQuery<MapGridComponent> MapGridQuery;

    private static readonly ProtoId<TagPrototype> FootstepSoundTag = "FootstepSound";

    private bool _relativeMovement;
    private float _minDamping;
    private float _airDamping;
    private float _offGridDamping;

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
        PullableQuery = GetEntityQuery<PullableComponent>();
        XformQuery = GetEntityQuery<TransformComponent>();
        NoRotateQuery = GetEntityQuery<NoRotateOnMoveComponent>();
        CanMoveInAirQuery = GetEntityQuery<CanMoveInAirComponent>();
        FootstepModifierQuery = GetEntityQuery<FootstepModifierComponent>();
        MapGridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<MovementSpeedModifierComponent, TileFrictionEvent>(OnTileFriction);

        InitializeInput();
        InitializeRelay();
        Subs.CVar(_configManager, CCVars.RelativeMovement, value => _relativeMovement = value, true);
        Subs.CVar(_configManager, CCVars.MinFriction, value => _minDamping = value, true);
        Subs.CVar(_configManager, CCVars.AirFriction, value => _airDamping = value, true);
        Subs.CVar(_configManager, CCVars.OffgridFriction, value => _offGridDamping = value, true);
        UpdatesBefore.Add(typeof(TileFrictionController));
    }

    public override void Shutdown()
    {
        base.Shutdown();
        ShutdownInput();
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
        Entity<InputMoverComponent> entity,
        float frameTime)
    {
        var uid = entity.Owner;
        var mover = entity.Comp;

        // If we're a relay then apply all of our data to the parent instead and go next.
        if (RelayQuery.TryComp(uid, out var relay))
        {
            if (!MoverQuery.TryComp(relay.RelayEntity, out var relayTargetMover))
                return;

            // Always lerp rotation so relay entities aren't cooked.
            LerpRotation(uid, mover, frameTime);
            var dirtied = false;

            if (relayTargetMover.RelativeEntity != mover.RelativeEntity)
            {
                relayTargetMover.RelativeEntity = mover.RelativeEntity;
                dirtied = true;
            }

            if (relayTargetMover.RelativeRotation != mover.RelativeRotation)
            {
                relayTargetMover.RelativeRotation = mover.RelativeRotation;
                dirtied = true;
            }

            if (relayTargetMover.TargetRelativeRotation != mover.TargetRelativeRotation)
            {
                relayTargetMover.TargetRelativeRotation = mover.TargetRelativeRotation;
                dirtied = true;
            }

            if (relayTargetMover.CanMove != mover.CanMove)
            {
                relayTargetMover.CanMove = mover.CanMove;
                dirtied = true;
            }

            if (dirtied)
            {
                Dirty(relay.RelayEntity, relayTargetMover);
            }

            return;
        }

        if (!XformQuery.TryComp(entity.Owner, out var xform))
            return;

        RelayTargetQuery.TryComp(uid, out var relayTarget);
        var relaySource = relayTarget?.Source;

        // If we're not the target of a relay then handle lerp data.
        if (relaySource == null)
        {
            // Update relative movement
            if (mover.LerpTarget < Timing.CurTime)
            {
                TryUpdateRelative(uid, mover, xform);
            }

            LerpRotation(uid, mover, frameTime);
        }

        // If we can't move then just use tile-friction / no movement handling.
        if (!mover.CanMove
            || !PhysicsQuery.TryComp(uid, out var physicsComponent)
            || PullableQuery.TryGetComponent(uid, out var pullable) && pullable.BeingPulled)
        {
            UsedMobMovement[uid] = false;
            return;
        }

        // If the body is in air but isn't weightless then it can't move
        // TODO: MAKE ISWEIGHTLESS EVENT BASED
        var weightless = _gravity.IsWeightless(uid, physicsComponent, xform);
        var inAirHelpless = false;

        if (physicsComponent.BodyStatus != BodyStatus.OnGround && !CanMoveInAirQuery.HasComponent(uid))
        {
            if (!weightless)
            {
                UsedMobMovement[uid] = false;
                return;
            }
            inAirHelpless = true;
        }

        UsedMobMovement[uid] = true;

        var moveSpeedComponent = ModifierQuery.CompOrNull(uid);

        float friction;
        float accel;
        Vector2 wishDir;
        var velocity = physicsComponent.LinearVelocity;

        // Get current tile def for things like speed/friction mods
        ContentTileDefinition? tileDef = null;

        var touching = false;
        // Whether we use tilefriction or not
        if (weightless || inAirHelpless)
        {
            // Find the speed we should be moving at and make sure we're not trying to move faster than that
            var walkSpeed = moveSpeedComponent?.WeightlessWalkSpeed ?? MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
            var sprintSpeed = moveSpeedComponent?.WeightlessSprintSpeed ?? MovementSpeedModifierComponent.DefaultBaseSprintSpeed;

            wishDir = AssertValidWish(mover, walkSpeed, sprintSpeed);

            var ev = new CanWeightlessMoveEvent(uid);
            RaiseLocalEvent(uid, ref ev, true);

            touching = ev.CanMove || xform.GridUid != null || MapGridQuery.HasComp(xform.GridUid);

            // If we're not on a grid, and not able to move in space check if we're close enough to a grid to touch.
            if (!touching && MobMoverQuery.TryComp(uid, out var mobMover))
                touching |= IsAroundCollider(PhysicsSystem, xform, mobMover, uid, physicsComponent);

            // If we're touching then use the weightless values
            if (touching)
            {
                touching = true;
                if (wishDir != Vector2.Zero)
                    friction = moveSpeedComponent?.WeightlessFriction ?? _airDamping;
                else
                    friction = moveSpeedComponent?.WeightlessFrictionNoInput ?? _airDamping;
            }
            // Otherwise use the off-grid values.
            else
            {
                friction = moveSpeedComponent?.OffGridFriction ?? _offGridDamping;
            }

            accel = moveSpeedComponent?.WeightlessAcceleration ?? MovementSpeedModifierComponent.DefaultWeightlessAcceleration;
        }
        else
        {
            if (MapGridQuery.TryComp(xform.GridUid, out var gridComp)
                && _mapSystem.TryGetTileRef(xform.GridUid.Value, gridComp, xform.Coordinates, out var tile)
                && physicsComponent.BodyStatus == BodyStatus.OnGround)
                tileDef = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];

            var walkSpeed = moveSpeedComponent?.CurrentWalkSpeed ?? MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
            var sprintSpeed = moveSpeedComponent?.CurrentSprintSpeed ?? MovementSpeedModifierComponent.DefaultBaseSprintSpeed;

            wishDir = AssertValidWish(mover, walkSpeed, sprintSpeed);

            if (wishDir != Vector2.Zero)
            {
                friction = moveSpeedComponent?.Friction ?? MovementSpeedModifierComponent.DefaultFriction;
                friction *= tileDef?.MobFriction ?? tileDef?.Friction ?? 1f;
            }
            else
            {
                friction = moveSpeedComponent?.FrictionNoInput ?? MovementSpeedModifierComponent.DefaultFrictionNoInput;
                friction *= tileDef?.Friction ?? 1f;
            }

            accel = moveSpeedComponent?.Acceleration ?? MovementSpeedModifierComponent.DefaultAcceleration;
            accel *= tileDef?.MobAcceleration ?? 1f;
        }

        // This way friction never exceeds acceleration when you're trying to move.
        // If you want to slow down an entity with "friction" you shouldn't be using this system.
        if (wishDir != Vector2.Zero)
            friction = Math.Min(friction, accel);
        friction = Math.Max(friction, _minDamping);
        var minimumFrictionSpeed = moveSpeedComponent?.MinimumFrictionSpeed ?? MovementSpeedModifierComponent.DefaultMinimumFrictionSpeed;
        Friction(minimumFrictionSpeed, frameTime, friction, ref velocity);

        if (!weightless || touching)
            Accelerate(ref velocity, in wishDir, accel, frameTime);

        SetWishDir((uid, mover), wishDir);

        /*
         * SNAKING!!! >-( 0 ================>
         * Snaking is a feature where you can move faster by strafing in a direction perpendicular to the
         * direction you intend to move while still holding the movement key for the direction you're trying to move.
         * Snaking only works if acceleration exceeds friction, and it's effectiveness scales as acceleration continues
         * to exceed friction.
         * Snaking works because friction is applied first in the direction of our current velocity, while acceleration
         * is applied after in our "Wish Direction" and is capped by the dot of our wish direction and current direction.
         * This means when you change direction, you're technically able to accelerate more than what the velocity cap
         * allows, but friction normally eats up the extra movement you gain.
         * By strafing as stated above you can increase your speed by about 1.4 (square root of 2).
         * This only works if friction is low enough so be sure that anytime you are letting a mob move in a low friction
         * environment you take into account the fact they can snake! Also be sure to lower acceleration as well to
         * prevent jerky movement!
         */
        PhysicsSystem.SetLinearVelocity(uid, velocity, body: physicsComponent);

        // Ensures that players do not spiiiiiiin
        PhysicsSystem.SetAngularVelocity(uid, 0, body: physicsComponent);

        // Handle footsteps at the end
        if (wishDir != Vector2.Zero)
        {
            if (!NoRotateQuery.HasComponent(uid))
            {
                // TODO apparently this results in a duplicate move event because "This should have its event run during
                // island solver"??. So maybe SetRotation needs an argument to avoid raising an event?
                var worldRot = _transform.GetWorldRotation(xform);

                _transform.SetLocalRotation(uid, xform.LocalRotation + wishDir.ToWorldAngle() - worldRot, xform);
            }

            if (!weightless && MobMoverQuery.TryGetComponent(uid, out var mobMover) &&
                TryGetSound(weightless, uid, mover, mobMover, xform, out var sound, tileDef: tileDef))
            {
                var soundModifier = mover.Sprinting ? 3.5f : 1.5f;

                var audioParams = sound.Params
                    .WithVolume(sound.Params.Volume + soundModifier)
                    .WithVariation(sound.Params.Variation ?? mobMover.FootstepVariation);

                // If we're a relay target then predict the sound for all relays.
                if (relaySource != null)
                {
                    _audio.PlayPredicted(sound, uid, relaySource.Value, audioParams);
                }
                else
                {
                    _audio.PlayPredicted(sound, uid, uid, audioParams);
                }
            }
        }
    }

    public Vector2 GetWishDir(Entity<InputMoverComponent?> mover)
    {
        if (!MoverQuery.Resolve(mover.Owner, ref mover.Comp, false))
            return Vector2.Zero;

        return mover.Comp.WishDir;
    }

    public void SetWishDir(Entity<InputMoverComponent> mover, Vector2 wishDir)
    {
        if (mover.Comp.WishDir.Equals(wishDir))
            return;

        mover.Comp.WishDir = wishDir;
        Dirty(mover);
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

            mover.RelativeRotation = (mover.RelativeRotation + adjustment).FlipPositive();
            Dirty(uid, mover);
        }
        else if (!angleDiff.Equals(Angle.Zero))
        {
            mover.RelativeRotation = mover.TargetRelativeRotation.FlipPositive();
            Dirty(uid, mover);
        }
    }

    public void Friction(float minimumFrictionSpeed, float frameTime, float friction, ref Vector2 velocity)
    {
        var speed = velocity.Length();

        if (speed < minimumFrictionSpeed)
            return;

        // This equation is lifted from the Physics Island solver.
        // We re-use it here because Kinematic Controllers can't/shouldn't use the Physics Friction
        velocity *= Math.Clamp(1.0f - frameTime * friction, 0.0f, 1.0f);

    }

    /// <summary>
    /// Adjusts the current velocity to the target velocity based on the specified acceleration.
    /// </summary>
    public static void Accelerate(ref Vector2 currentVelocity, in Vector2 velocity, float accel, float frameTime)
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
                (TryComp(otherCollider.Owner, out PullableComponent? pullable) && pullable.BeingPulled))
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

        if (!CanSound() || !_tags.HasTag(uid, FootstepSoundTag))
            return false;

        var coordinates = xform.Coordinates;
        var distanceNeeded = mover.Sprinting
            ? mobMover.StepSoundMoveDistanceRunning
            : mobMover.StepSoundMoveDistanceWalking;

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

        if (FootstepModifierQuery.TryComp(uid, out var moverModifier))
        {
            sound = moverModifier.FootstepSoundCollection;
            return sound != null;
        }

        if (_inventory.TryGetSlotEntity(uid, "shoes", out var shoes) &&
            FootstepModifierQuery.TryComp(shoes, out var modifier))
        {
            sound = modifier.FootstepSoundCollection;
            return sound != null;
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
        if (!MapGridQuery.TryComp(xform.GridUid, out var grid))
        {
            if (FootstepModifierQuery.TryComp(xform.MapUid, out var modifier))
            {
                sound = modifier.FootstepSoundCollection;
            }

            return sound != null;
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

            if (FootstepModifierQuery.TryComp(maybeFootstep, out var footstep))
            {
                sound = footstep.FootstepSoundCollection;
                return sound != null;
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

    private Vector2 AssertValidWish(InputMoverComponent mover, float walkSpeed, float sprintSpeed)
    {
        var (walkDir, sprintDir) = GetVelocityInput(mover);

        var total = walkDir * walkSpeed + sprintDir * sprintSpeed;

        var parentRotation = GetParentGridAngle(mover);
        var wishDir = _relativeMovement ? parentRotation.RotateVec(total) : total;

        DebugTools.Assert(MathHelper.CloseToPercent(total.Length(), wishDir.Length()));

        return wishDir;
    }

    private void OnTileFriction(Entity<MovementSpeedModifierComponent> ent, ref TileFrictionEvent args)
    {
        if (!TryComp<PhysicsComponent>(ent, out var physicsComponent) || !XformQuery.TryComp(ent, out var xform))
            return;

        // TODO: Make IsWeightless event based!!!
        if (physicsComponent.BodyStatus != BodyStatus.OnGround || _gravity.IsWeightless(ent, physicsComponent, xform))
            args.Modifier *= ent.Comp.BaseWeightlessFriction;
        else
            args.Modifier *= ent.Comp.BaseFriction;
    }
}
