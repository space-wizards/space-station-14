using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Throwing;
using Content.Shared.Toggleable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Misc;

public abstract partial class SharedTetherGunSystem : EntitySystem
{
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private MobStateSystem _mob = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedJointSystem _joints = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] protected SharedTransformSystem TransformSystem = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private ThrownItemSystem _thrown = default!;
    [Dependency] private IMapManager _mapManager = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private IGameTiming _timing = default!;
    private const string TetherJoint = "tether";
    private const string TetherJointMirror = "tetherMirror";

    private const float SpinVelocity = MathF.PI;
    private const float AngularChange = 1f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BaseForceGunComponent, ActivateInWorldEvent>(OnTetherActivate);
        SubscribeLocalEvent<ForceGunComponent, AfterInteractEvent>(OnForceRanged);
        SubscribeLocalEvent<BaseForceGunComponent, AfterInteractEvent>(OnTetherRanged);
        SubscribeLocalEvent<BaseForceGunComponent, DroppedEvent>(OnTetherGunDropped);
        SubscribeAllEvent<RequestTetherMoveEvent>(OnTetherMove);

        SubscribeLocalEvent<TetheredComponent, BuckleAttemptEvent>(OnTetheredBuckleAttempt);
        SubscribeLocalEvent<TetheredComponent, UpdateCanMoveEvent>(OnTetheredUpdateCanMove);
        SubscribeLocalEvent<TetheredComponent, EntGotInsertedIntoContainerMessage>(OnTetheredContainerInserted);
        UpdatesBefore.Add(typeof(SharedJointSystem));
    }

    private void OnTetheredContainerInserted(
        EntityUid uid,
        TetheredComponent component,
        EntGotInsertedIntoContainerMessage args
    )
    {
        if (TryComp<BaseForceGunComponent>(component.Tetherer, out var tetherGun))
        {
            StopTether(component.Tetherer, tetherGun);
        }
    }

    private void OnTetheredBuckleAttempt(EntityUid uid, TetheredComponent component, ref BuckleAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnTetheredUpdateCanMove(EntityUid uid, TetheredComponent component, UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateTetheredRotation(frameTime);
        UpdateGunTethers();
    }

    private void UpdateTetheredRotation(float frameTime)
    {
        // Corrects angular velocity each tick due to joint funnies
        var tetheredQuery = EntityQueryEnumerator<TetheredComponent, PhysicsComponent>();
        while (tetheredQuery.MoveNext(out var uid, out _, out var physics))
        {
            var sign = Math.Sign(physics.AngularVelocity);
            if (sign == 0)
                sign = 1;

            var targetVelocity = MathF.PI * sign;
            var shortFall = Math.Clamp(targetVelocity - physics.AngularVelocity, -SpinVelocity, SpinVelocity);
            shortFall *= frameTime * AngularChange;
            _physics.ApplyAngularImpulse(uid, shortFall, body: physics);
        }
    }

    private void UpdateGunTethers()
    {
        // Moves mirror so the force applied to the gun equals the force on the object.
        // Also stops the tether if over max beam length.
        var gunQuery = EntityQueryEnumerator<BaseForceGunComponent>();
        while (gunQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.Tethered == null || comp.TetherEntity == null || comp.TetherMirrorEntity == null)
                continue;
            if (!Exists(comp.Tethered) || !Exists(comp.TetherEntity) || !Exists(comp.TetherMirrorEntity))
            {
                StopTether(uid, comp);
                continue;
            }

            var mapGunCoords = TransformSystem.GetMapCoordinates(uid);
            if (!TryGetCoords(mapGunCoords, out var gunCoords))
                continue;
            var mapTetheredCoords = TransformSystem.GetMapCoordinates(comp.Tethered.Value);
            if (!TryGetCoords(mapTetheredCoords, out var tetheredCoords))
                continue;

            if (!gunCoords.TryDistance(EntityManager, TransformSystem, tetheredCoords, out var distance)
                || distance > comp.MaxBeamLength)
            {
                StopTether(uid, comp, land: true);
                continue;
            }

            // When the gun isn't in hand, clamp the tether entity within range of the gun
            if (!TryGetGun(Transform(uid).ParentUid, out var gun, out _) || gun != uid)
            {
                var coords = GetAllowedTetherEntityCoords(gunCoords, tetheredCoords, comp);
                if (coords != tetheredCoords)
                    TransformSystem.SetCoordinates(comp.TetherEntity.Value, coords);
                    Dirty(comp.TetherEntity.Value, Transform(comp.TetherEntity.Value));
            }

            MoveMirrorEntity(uid, comp.Tethered, comp.TetherEntity, comp.TetherMirrorEntity);
        }
    }

    private void MoveMirrorEntity(EntityUid uid, EntityUid? tethered, EntityUid? tetherEntity, EntityUid? mirrorEntity)
    {
        if (!Exists(tethered) || !Exists(tetherEntity) || !Exists(mirrorEntity))
            return;

        var gunCoords = TransformSystem.GetMapCoordinates(uid);
        var tetheredCoords = TransformSystem.GetMapCoordinates(tethered.Value);
        var tetherCoords = TransformSystem.GetMapCoordinates(tetherEntity.Value);
        var targetCoords = gunCoords.Offset(tetheredCoords.Position - tetherCoords.Position);
        var currentCoords = TransformSystem.GetMapCoordinates(mirrorEntity.Value);

        if ((targetCoords.Position - currentCoords.Position).Length() <= 0.01f)
            return;
        if (!TryGetCoords(targetCoords, out var coords))
            return;
        TransformSystem.SetCoordinates(mirrorEntity.Value, coords);
        Dirty(mirrorEntity.Value, Transform(mirrorEntity.Value));
    }

    private void OnTetherGunDropped(EntityUid uid, BaseForceGunComponent component, DroppedEvent args)
    {
        // When the tether gun is dropped the tether entity is placed at the midpoint of the gun and object
        // Needed to prevent some strange looking movement of a dropped gun
        // Could instead have the tether entity moved to the target
        if (_timing.ApplyingState)
            return;
        if (component.Tethered == null || component.TetherEntity == null || component.TetherMirrorEntity == null)
            return;
        var tetheredCoords = TransformSystem.GetMapCoordinates(component.Tethered.Value);
        if (!TryGetCoords(tetheredCoords, out var coords))
            return;
        // TODO: Based on gun location before dropping
        // Should be from after dropping
        if (TryComp(uid, out TransformComponent? gunXform)
            && coords.TryDelta(EntityManager, TransformSystem, gunXform.Coordinates, out Vector2 delta))
        {
            var midpoint = gunXform.Coordinates.Offset(delta / 2);
            // If the midpoint is outside the gun's allowed range, clamp it to the edge
            coords = GetAllowedTetherEntityCoords(gunXform.Coordinates, midpoint, component);
        }
        TransformSystem.SetCoordinates(component.TetherEntity.Value, coords);
    }

    public bool TryGetCoords(MapCoordinates mapCoords, out EntityCoordinates coords)
    {
        if (_mapManager.TryFindGridAt(mapCoords, out var gridUid, out _))
        {
            coords = TransformSystem.ToCoordinates(gridUid, mapCoords);
            return true;
        }

        var map = _mapSystem.GetMapOrInvalid(mapCoords.MapId);
        if (map == EntityUid.Invalid)
        {
            coords = default;
            return false;
        }

        coords = TransformSystem.ToCoordinates(map, mapCoords);
        return true;
    }

    public EntityCoordinates GetAllowedTetherEntityCoords(
        EntityCoordinates gunCoords,
        EntityCoordinates desireCoords,
        BaseForceGunComponent comp
    )
    {
        var desiredMap = TransformSystem.ToMapCoordinates(desireCoords);
        var gunMap = TransformSystem.ToMapCoordinates(gunCoords);

        if (desiredMap.MapId != gunMap.MapId)
            return gunCoords;

        var delta = desiredMap.Position - gunMap.Position;
        if (delta.Length() < comp.MaxDistance)
            return desireCoords;

        if (!TryGetCoords(gunMap.Offset(delta.Normalized() * comp.MaxDistance), out var coords))
            return gunCoords;
        return coords;
    }

    private void OnTetherMove(RequestTetherMoveEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null)
            return;

        if (
            !TryGetGun(user.Value, out var gunUid, out var gun)
            || gun.TetherEntity == null
            || gun.TetherMirrorEntity == null
        )
        {
            return;
        }

        var coords = GetCoordinates(msg.Coordinates);

        coords = GetAllowedTetherEntityCoords(Transform(gunUid.Value).Coordinates, coords, gun);

        TransformSystem.SetCoordinates(gun.TetherEntity.Value, coords);
    }

    private void OnTetherRanged(EntityUid uid, BaseForceGunComponent component, AfterInteractEvent args)
    {
        // Whenever any tether gun clicks on something
        // The order of events triggering may not be correct
        if (args.Target == null || args.Handled)
            return;
        if (TryTether(uid, args.Target.Value, args.User, component))
            args.Handled = true;
    }

    private void OnForceRanged(EntityUid uid, ForceGunComponent forceComponent, AfterInteractEvent args)
    {
        // Whenever any force gun clicks on something
        if (args.Handled)
            return;
        if (!TryComp<BaseForceGunComponent>(uid, out var baseComponent))
        {
            return;
        }
        if (baseComponent.Tethered == null)
            return;
        var tethered = baseComponent.Tethered.Value;
        StopTether(uid, baseComponent, land: false);
        if (!_netManager.IsServer)
            return;
        var thrownPos = TransformSystem.GetMapCoordinates(tethered);
        var mapPos = TransformSystem.ToMapCoordinates(args.ClickLocation);
        var direction = mapPos.Position - thrownPos.Position;
        // pushbackRatio is 0 here; reverse impulse is applied manually below
        _throwing.TryThrow(
            tethered,
            direction,
            forceComponent.ThrowSpeed,
            user: Transform(uid).ParentUid,
            playSound: false,
            pushbackRatio: 0
        );
        _audio.PlayPredicted(forceComponent.LaunchSound, uid, null);
        args.Handled = true;
        if (!TryComp(uid, out PhysicsComponent? body) || !TryComp(tethered, out PhysicsComponent? physics))
            return;
        var physicsQuery = GetEntityQuery<PhysicsComponent>();
        var parents = new List<PhysicsComponent>();
        // If the forcegun is inside a container it should still apply the force
        // This is because it will be in someones hand
        // This also prevents the bug of firing a locker you are in
        if (_container.TryFindComponentsOnEntityContainerOrParent(uid, physicsQuery, parents))
            body = parents.LastOrDefault() ?? body;
        var impulseVector = direction.Normalized() * forceComponent.ThrowSpeed * physics.Mass * (baseComponent.ReverseForce ? 1 : 0);
        _physics.ApplyLinearImpulse(body.Owner, -impulseVector, body: body);
    }

    protected bool TryGetGun(
        EntityUid user,
        [NotNullWhen(true)] out EntityUid? gunUid,
        [NotNullWhen(true)] out BaseForceGunComponent? gun
    )
    {
        gunUid = null;
        gun = null;

        if (
            !_hands.TryGetActiveItem(user, out var activeItem)
            || !TryComp(activeItem, out gun)
            || _container.IsEntityInContainer(user)
        )
        {
            return false;
        }

        gunUid = activeItem.Value;
        return true;
    }

    private void OnTetherActivate(EntityUid uid, BaseForceGunComponent component, ActivateInWorldEvent args)
    {
        if (!args.Complex)
            return;

        StopTether(uid, component);
    }

    public bool TryTether(EntityUid gun, EntityUid target, EntityUid? user, BaseForceGunComponent? component = null)
    {
        if (!Resolve(gun, ref component))
            return false;

        if (!CanTether(gun, component, target, user))
            return false;

        StartTether(gun, component, target, user);
        return true;
    }

    protected virtual bool CanTether(EntityUid uid, BaseForceGunComponent component, EntityUid target, EntityUid? user)
    {
        if (HasComp<TetheredComponent>(target) || !TryComp<PhysicsComponent>(target, out var physics))
            return false;

        if ((physics.BodyType == BodyType.Static && !component.CanUnanchor) || _container.IsEntityInContainer(target))
            return false;

        if (physics.Mass > component.MassLimit)
            return false;

        if (!component.CanTetherAlive && _mob.IsAlive(target) || target == Transform(uid).ParentUid)
            return false;

        if (TryComp<StrapComponent>(target, out var strap) && strap.BuckledEntities.Count > 0)
            return false;

        var mapGunCoords = TransformSystem.GetMapCoordinates(uid);
        if (!TryGetCoords(mapGunCoords, out var gunCoords))
            return false;
        var mapTargetCoords = TransformSystem.GetMapCoordinates(target);
        if (!TryGetCoords(mapTargetCoords, out var targetCoords))
            return false;
        if (
            !gunCoords.TryDistance(EntityManager, TransformSystem, targetCoords, out var distance)
            || distance > component.MaxBeamLength
        )
        {
            return false;
        }

        return true;
    }

    protected virtual void StartTether(
        EntityUid gunUid,
        BaseForceGunComponent component,
        EntityUid target,
        EntityUid? user,
        PhysicsComponent? targetPhysics = null,
        TransformComponent? targetXform = null
    )
    {
        if (!Resolve(target, ref targetPhysics, ref targetXform))
            return;

        if (component.Tethered != null)
        {
            StopTether(gunUid, component, transfer: true);
        }

        UpdateSprite(gunUid, true);

        // Target updates
        TransformSystem.Unanchor(target, targetXform);
        component.Tethered = target;
        var tethered = EnsureComp<TetheredComponent>(target);
        _physics.SetBodyStatus(target, targetPhysics, BodyStatus.InAir, false);
        _physics.SetSleepingAllowed(target, targetPhysics, false);
        tethered.Tetherer = gunUid;
        _physics.SetAngularVelocity(target, SpinVelocity, body: targetPhysics);
        _physics.WakeBody(target, body: targetPhysics);
        var thrown = EnsureComp<ThrownItemComponent>(target);
        thrown.Thrower = gunUid;
        _blocker.UpdateCanMove(target);

        // Invisible tether entity
        // The properties on both joints need to be the same to ensure the force is the same
        var tether = PredictedSpawnAtPosition("TetherEntity", new EntityCoordinates(target, new Vector2(0, 0)));
        var tetherPhysics = Comp<PhysicsComponent>(tether);
        var mirrorTether = PredictedSpawnAtPosition("TetherEntity", new EntityCoordinates(gunUid, new Vector2(0, 0)));
        component.TetherEntity = tether;
        component.TetherMirrorEntity = mirrorTether;

        var joint = _joints.CreateMouseJoint(tether, target, id: TetherJoint);

        SharedJointSystem.LinearStiffness(
            component.Frequency,
            component.DampingRatio,
            tetherPhysics.Mass,
            targetPhysics.Mass,
            out var stiffness,
            out var damping
        );
        joint.Stiffness = stiffness;
        joint.Damping = damping;
        joint.MaxForce = component.MaxForce;
        if (component.ReverseForce)
        {
            var jointMirror = _joints.CreateMouseJoint(mirrorTether, gunUid, id: TetherJointMirror);
            jointMirror.Stiffness = stiffness;
            jointMirror.Damping = damping;
            jointMirror.MaxForce = component.MaxForce;
        }

        // Sad...
        if (_netManager.IsServer && component.Stream == null)
            component.Stream = _audio.PlayPredicted(component.Sound, gunUid, null)?.Entity;

        if (!Exists(component.Tethered))
            StopTether(gunUid, component);
        Dirty(target, tethered);
        Dirty(gunUid, component);
    }

    protected virtual void StopTether(
        EntityUid gunUid,
        BaseForceGunComponent component,
        bool land = true,
        bool transfer = false
    )
    {
        CleanupTetherEntities(component);

        if (component.Tethered == null)
            return;
        if (!Exists(component.Tethered))
        {
            component.Tethered = null;
            UpdateSprite(gunUid, false);
            Dirty(gunUid, component);
            return;
        }

        var tethered = component.Tethered.Value;

        if (TryComp<PhysicsComponent>(tethered, out var targetPhysics))
        {
            if (land)
            {
                var thrown = EnsureComp<ThrownItemComponent>(tethered);
                _thrown.LandComponent(tethered, thrown, targetPhysics, true);
                _thrown.StopThrow(tethered, thrown);
            }

            _physics.SetBodyStatus(tethered, targetPhysics, BodyStatus.OnGround);
            _physics.SetSleepingAllowed(tethered, targetPhysics, true);
        }

        if (!transfer)
        {
            _audio.Stop(component.Stream);
            component.Stream = null;
        }

        UpdateSprite(gunUid, false);

        RemComp<TetheredComponent>(tethered);
        _blocker.UpdateCanMove(tethered);
        component.Tethered = null;
        Dirty(gunUid, component);
    }

    private void CleanupTetherEntities(BaseForceGunComponent component)
    {
        if (component.TetherEntity == null && component.TetherMirrorEntity == null)
            return;

        if (component.TetherEntity != null && Exists(component.TetherEntity))
            _joints.ClearJoints(component.TetherEntity.Value);
        if (component.TetherMirrorEntity != null && Exists(component.TetherMirrorEntity))
            _joints.ClearJoints(component.TetherMirrorEntity.Value);

        PredictedQueueDel(component.TetherEntity);
        PredictedQueueDel(component.TetherMirrorEntity);
        component.TetherEntity = null;
        component.TetherMirrorEntity = null;
    }

    private void UpdateSprite(EntityUid uid, bool toggle)
    {
        TryComp<AppearanceComponent>(uid, out var appearance);
        _appearance.SetData(uid, TetherVisualsStatus.Key, toggle, appearance);
        _appearance.SetData(uid, ToggleableVisuals.Enabled, toggle, appearance);
    }

    [Serializable, NetSerializable]
    protected sealed class RequestTetherMoveEvent : EntityEventArgs
    {
        public NetCoordinates Coordinates;
    }

    [Serializable, NetSerializable]
    public enum TetherVisualsStatus : byte
    {
        Key,
    }
}
