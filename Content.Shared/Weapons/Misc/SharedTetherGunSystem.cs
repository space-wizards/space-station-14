using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Throwing;
using Content.Shared.Toggleable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Misc;

public abstract partial class SharedTetherGunSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly ThrownItemSystem _thrown = default!;

    private const string TetherJoint = "tether";

    private const float SpinVelocity = MathF.PI;
    private const float AngularChange = 1f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TetherGunComponent, ActivateInWorldEvent>(OnTetherActivate);
        SubscribeLocalEvent<TetherGunComponent, AfterInteractEvent>(OnTetherRanged);
        SubscribeAllEvent<RequestTetherMoveEvent>(OnTetherMove);

        SubscribeLocalEvent<TetheredComponent, BuckleAttemptEvent>(OnTetheredBuckleAttempt);
        SubscribeLocalEvent<TetheredComponent, UpdateCanMoveEvent>(OnTetheredUpdateCanMove);
        SubscribeLocalEvent<TetheredComponent, EntGotInsertedIntoContainerMessage>(OnTetheredContainerInserted);

        InitializeForce();
    }

    private void OnTetheredContainerInserted(EntityUid uid, TetheredComponent component, EntGotInsertedIntoContainerMessage args)
    {
        if (TryComp<TetherGunComponent>(component.Tetherer, out var tetherGun))
        {
            StopTether(component.Tetherer, tetherGun);
            return;
        }

        if (TryComp<ForceGunComponent>(component.Tetherer, out var forceGun))
        {
            StopTether(component.Tetherer, forceGun);
            return;
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
    }

    private void OnTetherMove(RequestTetherMoveEvent msg, EntitySessionEventArgs args)
    {
        var user = args.SenderSession.AttachedEntity;

        if (user == null)
            return;

        if (!TryGetTetherGun(user.Value, out var gunUid, out var gun) || gun.TetherEntity == null)
        {
            return;
        }

        var coords = GetCoordinates(msg.Coordinates);

        if (!coords.TryDistance(EntityManager, TransformSystem, Transform(gunUid.Value).Coordinates,
                out var distance) ||
            distance > gun.MaxDistance)
        {
            return;
        }

        TransformSystem.SetCoordinates(gun.TetherEntity.Value, coords);
    }

    private void OnTetherRanged(EntityUid uid, TetherGunComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled)
            return;

        TryTether(uid, args.Target.Value, args.User, component);
    }

    protected bool TryGetTetherGun(EntityUid user, [NotNullWhen(true)] out EntityUid? gunUid, [NotNullWhen(true)] out TetherGunComponent? gun)
    {
        gunUid = null;
        gun = null;

        if (!TryComp<HandsComponent>(user, out var hands) ||
            !TryComp(hands.ActiveHandEntity, out gun) ||
            _container.IsEntityInContainer(user))
        {
            return false;
        }

        gunUid = hands.ActiveHandEntity.Value;
        return true;
    }

    private void OnTetherActivate(EntityUid uid, TetherGunComponent component, ActivateInWorldEvent args)
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

        if (physics.BodyType == BodyType.Static && !component.CanUnanchor ||
            _container.IsEntityInContainer(target))
            return false;

        if (physics.Mass > component.MassLimit)
            return false;

        if (!component.CanTetherAlive && _mob.IsAlive(target))
            return false;

        if (TryComp<StrapComponent>(target, out var strap) && strap.BuckledEntities.Count > 0)
            return false;

        return true;
    }

    protected virtual void StartTether(EntityUid gunUid, BaseForceGunComponent component, EntityUid target, EntityUid? user,
        PhysicsComponent? targetPhysics = null, TransformComponent? targetXform = null)
    {
        if (!Resolve(target, ref targetPhysics, ref targetXform))
            return;

        if (component.Tethered != null)
        {
            StopTether(gunUid, component, true);
        }

        TryComp<AppearanceComponent>(gunUid, out var appearance);
        _appearance.SetData(gunUid, TetherVisualsStatus.Key, true, appearance);
        _appearance.SetData(gunUid, ToggleableLightVisuals.Enabled, true, appearance);

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
        var thrown = EnsureComp<ThrownItemComponent>(component.Tethered.Value);
        thrown.Thrower = gunUid;
        _blocker.UpdateCanMove(target);

        // Invisible tether entity
        var tether = Spawn("TetherEntity", TransformSystem.GetMapCoordinates(target));
        var tetherPhysics = Comp<PhysicsComponent>(tether);
        component.TetherEntity = tether;
        _physics.WakeBody(tether);

        var joint = _joints.CreateMouseJoint(tether, target, id: TetherJoint);

        SharedJointSystem.LinearStiffness(component.Frequency, component.DampingRatio, tetherPhysics.Mass, targetPhysics.Mass, out var stiffness, out var damping);
        joint.Stiffness = stiffness;
        joint.Damping = damping;
        joint.MaxForce = component.MaxForce;

        // Sad...
        if (_netManager.IsServer && component.Stream == null)
            component.Stream = _audio.PlayPredicted(component.Sound, gunUid, null)?.Entity;

        Dirty(target, tethered);
        Dirty(gunUid, component);
    }

    protected virtual void StopTether(EntityUid gunUid, BaseForceGunComponent component, bool land = true, bool transfer = false)
    {
        if (component.Tethered == null)
            return;

        if (component.TetherEntity != null)
        {
            _joints.RemoveJoint(component.TetherEntity.Value, TetherJoint);

            if (_netManager.IsServer)
                QueueDel(component.TetherEntity.Value);

            component.TetherEntity = null;
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
            _physics.SetAngularDamping(component.Tethered.Value, targetPhysics, Comp<TetheredComponent>(component.Tethered.Value).OriginalAngularDamping);
        }

        if (!transfer)
        {
            _audio.Stop(component.Stream);
            component.Stream = null;
        }

        TryComp<AppearanceComponent>(gunUid, out var appearance);
        _appearance.SetData(gunUid, TetherVisualsStatus.Key, false, appearance);
        _appearance.SetData(gunUid, ToggleableLightVisuals.Enabled, false, appearance);

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
