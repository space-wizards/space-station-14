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

        // Just to set the angular velocity due to joint funnies
        var tetheredQuery = EntityQueryEnumerator<TetheredComponent, PhysicsComponent>();

        while (tetheredQuery.MoveNext(out var uid, out _, out var physics))
        {
            var sign = Math.Sign(physics.AngularVelocity);

            if (sign == 0)
            {
                sign = 1;
            }

            var targetVelocity = MathF.PI * sign;

            var shortFall = Math.Clamp(targetVelocity - physics.AngularVelocity, -SpinVelocity, SpinVelocity);
            shortFall *= frameTime * AngularChange;

            _physics.ApplyAngularImpulse(uid, shortFall, body: physics);
        }

        var gunQuery = EntityQueryEnumerator<BaseForceGunComponent>();
        while (gunQuery.MoveNext(out var uid, out var comp))
        {
            if (comp.Tethered == null || comp.TetherEntity == null || comp.TetherMirrorEntity == null)
                continue;
            var mapGunCoords = TransformSystem.GetMapCoordinates(uid);
            var gunCoords = GetCoords(mapGunCoords);
            var mapTetheredCoords = TransformSystem.GetMapCoordinates(comp.Tethered.Value);
            var tetheredCoords = GetCoords(mapTetheredCoords);
            if (
                !gunCoords.TryDistance(EntityManager, TransformSystem, tetheredCoords, out var distance)
                || distance > comp.MaxBeamLength
            )
            {
                StopTether(uid, comp, land: true);
                continue;
            }
            if (!TryGetGun(Transform(uid).ParentUid, out var gun, out _) || gun != uid)
            {
                var coords = GetAllowedTetherEntityCoords(gunCoords, tetheredCoords, comp);
                if (coords != tetheredCoords)
                    TransformSystem.SetCoordinates(comp.TetherEntity.Value, coords);
            }
            MoveMirrorEntity(uid, comp.Tethered, comp.TetherEntity, comp.TetherMirrorEntity);
        }
    }

    private void MoveMirrorEntity(EntityUid uid, EntityUid? tethered, EntityUid? tetherEntity, EntityUid? mirrorEntity)
    {
        if (tethered == null || tetherEntity == null || mirrorEntity == null)
            return;

        var gunCoords = TransformSystem.GetMapCoordinates(uid);
        var tetheredCoords = TransformSystem.GetMapCoordinates(tethered.Value);
        var tetherCoords = TransformSystem.GetMapCoordinates(tetherEntity.Value);
        var targetCoords = gunCoords.Offset(tetheredCoords.Position - tetherCoords.Position);
        var currentCoords = TransformSystem.GetMapCoordinates(mirrorEntity.Value);

        if ((targetCoords.Position - currentCoords.Position).Length() <= 0.01f)
            return;
        var coords = GetCoords(targetCoords);

        TransformSystem.SetCoordinates(mirrorEntity.Value, coords);
    }

    private void OnTetherGunDropped(EntityUid uid, BaseForceGunComponent component, DroppedEvent args)
    {
        if (component.Tethered == null || component.TetherEntity == null || component.TetherMirrorEntity == null)
            return;
        var tetheredCoords = TransformSystem.GetMapCoordinates(component.Tethered.Value);
        var coords = GetCoords(tetheredCoords);
        // TODO: Based on gun location before dropping
        // Should be from after dropping
        if (TryComp(uid, out TransformComponent? gunXform))
        {
            if (coords.TryDelta(EntityManager, TransformSystem, gunXform.Coordinates, out Vector2 delta))
            {
                coords = gunXform.Coordinates.Offset(delta / 2);
                coords = GetAllowedTetherEntityCoords(gunXform.Coordinates, coords, component);
            }
        }
        TransformSystem.SetCoordinates(component.TetherEntity.Value, coords);
        MoveMirrorEntity(uid, component.Tethered, component.TetherEntity, component.TetherMirrorEntity);
    }

    public EntityCoordinates GetCoords(MapCoordinates mapCoords)
    {
        EntityCoordinates coords;
        if (_mapManager.TryFindGridAt(mapCoords, out var gridUid, out _))
        {
            coords = TransformSystem.ToCoordinates(gridUid, mapCoords);
        }
        else
        {
            coords = TransformSystem.ToCoordinates(_mapSystem.GetMap(mapCoords.MapId), mapCoords);
        }
        return coords;
    }

    public EntityCoordinates GetAllowedTetherEntityCoords(
        EntityCoordinates gunCoords,
        EntityCoordinates desireCoords,
        BaseForceGunComponent comp
    )
    {
        var desiredMap = TransformSystem.ToMapCoordinates(desireCoords).Position;
        var gunMap = TransformSystem.ToMapCoordinates(gunCoords).Position;
        var delta = desiredMap - gunMap;
        if (delta.Length() < comp.MaxDistance)
            return desireCoords;
        return GetCoords(TransformSystem.ToMapCoordinates(gunCoords).Offset(delta.Normalized() * comp.MaxDistance));
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
        MoveMirrorEntity(gunUid.Value, gun.Tethered, gun.TetherEntity, gun.TetherMirrorEntity);
    }

    private void OnTetherRanged(EntityUid uid, BaseForceGunComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled)
            return;
        if (TryTether(uid, args.Target.Value, args.User, component))
            args.Handled = true;
    }

    private void OnForceRanged(EntityUid uid, ForceGunComponent forceComponent, AfterInteractEvent args)
    {
        if (args.Handled)
            return;
        if (!TryComp<BaseForceGunComponent>(uid, out var baseComponent) || baseComponent == null)
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
        if (_container.TryFindComponentsOnEntityContainerOrParent(uid, physicsQuery, parents))
            body = parents.LastOrDefault();
        var impulseVector = direction.Normalized() * forceComponent.ThrowSpeed * physics.Mass * (baseComponent.ReverseForce ? 1 : 0);
        _physics.ApplyLinearImpulse( body!.Owner, -impulseVector, body: body);
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

        if (physics.BodyType == BodyType.Static && !component.CanUnanchor || _container.IsEntityInContainer(target))
            return false;

        if (physics.Mass > component.MassLimit)
            return false;

        if (!component.CanTetherAlive && _mob.IsAlive(target))
            return false;

        if (TryComp<StrapComponent>(target, out var strap) && strap.BuckledEntities.Count > 0)
            return false;

        var mapGunCoords = TransformSystem.GetMapCoordinates(uid);
        var gunCoords = GetCoords(mapGunCoords);
        var mapTargetCoords = TransformSystem.GetMapCoordinates(target);
        var targetCoords = GetCoords(mapTargetCoords);
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

        TryComp<AppearanceComponent>(gunUid, out var appearance);
        _appearance.SetData(gunUid, TetherVisualsStatus.Key, true, appearance);
        _appearance.SetData(gunUid, ToggleableVisuals.Enabled, true, appearance);

        // Target updates
        TransformSystem.Unanchor(target, targetXform);
        component.Tethered = target;
        var tethered = EnsureComp<TetheredComponent>(target);
        _physics.SetBodyStatus(target, targetPhysics, BodyStatus.InAir, false);
        _physics.SetSleepingAllowed(target, targetPhysics, false);
        tethered.Tetherer = gunUid;
        tethered.OriginalAngularDamping = targetPhysics.AngularDamping;
        _physics.SetAngularDamping(target, targetPhysics, 0f);
        _physics.SetLinearDamping(target, targetPhysics, 0f);
        _physics.SetAngularVelocity(target, SpinVelocity, body: targetPhysics);
        _physics.WakeBody(target, body: targetPhysics);
        _blocker.UpdateCanMove(target);

        // Invisible tether entity
        if (!_timing.ApplyingState)
        {
            var tether = Spawn("TetherEntity", TransformSystem.GetMapCoordinates(target));
            var tetherPhysics = Comp<PhysicsComponent>(tether);
            var mirrorTether = Spawn("TetherEntity", TransformSystem.GetMapCoordinates(gunUid));
            var mirrorPhysics = Comp<PhysicsComponent>(mirrorTether);
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
        }

        // Sad...
        if (_netManager.IsServer && component.Stream == null)
            component.Stream = _audio.PlayPredicted(component.Sound, gunUid, null)?.Entity;

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

        if (component.TetherEntity != null && component.TetherMirrorEntity != null)
        {
            if (!_timing.ApplyingState)
            {
                _joints.RemoveJoint(component.TetherEntity.Value, TetherJoint);
                _joints.RemoveJoint(component.TetherMirrorEntity.Value, TetherJointMirror);
            }
            if (_netManager.IsServer)
            {
                QueueDel(component.TetherEntity.Value);
                QueueDel(component.TetherMirrorEntity.Value);
            }
            component.TetherEntity = null;
            component.TetherMirrorEntity = null;
        }

        if (TryComp<PhysicsComponent>(component.Tethered, out var targetPhysics))
        {
            if (land)
            {
                var thrown = EnsureComp<ThrownItemComponent>(component.Tethered.Value);
                _thrown.LandComponent(component.Tethered.Value, thrown, targetPhysics, true);
                _thrown.StopThrow(component.Tethered.Value, thrown);
            }

            _physics.SetBodyStatus(component.Tethered.Value, targetPhysics, BodyStatus.OnGround);
            _physics.SetSleepingAllowed(component.Tethered.Value, targetPhysics, true);
            _physics.SetAngularDamping(
                component.Tethered.Value,
                targetPhysics,
                Comp<TetheredComponent>(component.Tethered.Value).OriginalAngularDamping
            );
        }

        if (!transfer)
        {
            _audio.Stop(component.Stream);
            component.Stream = null;
        }

        TryComp<AppearanceComponent>(gunUid, out var appearance);
        _appearance.SetData(gunUid, TetherVisualsStatus.Key, false, appearance);
        _appearance.SetData(gunUid, ToggleableVisuals.Enabled, false, appearance);

        RemComp<TetheredComponent>(component.Tethered.Value);
        _blocker.UpdateCanMove(component.Tethered.Value);
        component.Tethered = null;
        Dirty(gunUid, component);
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
