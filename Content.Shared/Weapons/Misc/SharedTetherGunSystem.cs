using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Misc;

public abstract class SharedTetherGunSystem : EntitySystem
{
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] private readonly ThrownItemSystem _thrown = default!;

    private const string TetherJoint = "tether";

    private const float SpinVelocity = 4f;
    private const float AngularChange = 1f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TetherGunComponent, ActivateInWorldEvent>(OnTetherActivate);
        SubscribeLocalEvent<TetherGunComponent, AfterInteractEvent>(OnTetherRanged);
        SubscribeAllEvent<RequestTetherMoveEvent>(OnTetherMove);
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

            var targetVelocity = SpinVelocity * sign;

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

        var mapCoords = msg.Coordinates.ToMap(EntityManager, TransformSystem);

        if (mapCoords.MapId != Transform(gunUid.Value).MapID)
            return;

        TransformSystem.SetCoordinates(gun.TetherEntity.Value, msg.Coordinates);
    }

    private void OnTetherRanged(EntityUid uid, TetherGunComponent component, AfterInteractEvent args)
    {
        if (args.Target == null || args.Handled)
            return;

        TryTether(uid, args.Target.Value, component);
    }

    protected bool TryGetTetherGun(EntityUid user, [NotNullWhen(true)] out EntityUid? gunUid, [NotNullWhen(true)] out TetherGunComponent? gun)
    {
        gunUid = null;
        gun = null;

        if (!TryComp<HandsComponent>(user, out var hands) ||
            !TryComp(hands.ActiveHandEntity, out gun))
        {
            return false;
        }

        gunUid = hands.ActiveHandEntity.Value;
        return true;
    }

    private void OnTetherActivate(EntityUid uid, TetherGunComponent component, ActivateInWorldEvent args)
    {
        StopTether(component);
    }

    public void TryTether(EntityUid gun, EntityUid target, TetherGunComponent? component = null)
    {
        if (!Resolve(gun, ref component))
            return;

        if (!CanTether(component, target))
            return;

        StartTether(gun, component, target);
    }

    private bool CanTether(TetherGunComponent component, EntityUid target)
    {
        if (HasComp<TetheredComponent>(target) || !TryComp<PhysicsComponent>(target, out var physics))
            return false;

        if (physics.BodyType == BodyType.Static && !component.CanUnanchor)
            return false;

        if (physics.Mass > component.MassLimit)
            return false;

        return true;
    }

    private void StartTether(EntityUid gunUid, TetherGunComponent component, EntityUid target,
        PhysicsComponent? targetPhysics = null, TransformComponent? targetXform = null)
    {
        if (!Resolve(target, ref targetPhysics, ref targetXform))
            return;

        if (component.Tethered != null)
        {
            StopTether(component);
        }

        // Target updates
        TransformSystem.Unanchor(target, targetXform);
        component.Tethered = target;
        var tethered = EnsureComp<TetheredComponent>(target);
        _physics.SetBodyStatus(targetPhysics, BodyStatus.InAir, false);
        _physics.SetSleepingAllowed(target, targetPhysics, false);
        tethered.Tetherer = gunUid;
        tethered.OriginalAngularDamping = targetPhysics.AngularDamping;
        _physics.SetAngularDamping(targetPhysics, 0f);
        _physics.SetLinearDamping(targetPhysics, 0f);
        _physics.SetAngularVelocity(target, SpinVelocity, body: targetPhysics);
        _physics.WakeBody(target, body: targetPhysics);
        var thrown = EnsureComp<ThrownItemComponent>(component.Tethered.Value);
        thrown.Thrower = gunUid;

        // Invisible tether entity
        var tether = Spawn("TetherEntity", Transform(target).MapPosition);
        var tetherPhysics = Comp<PhysicsComponent>(tether);
        component.TetherEntity = tether;
        _physics.WakeBody(tether);

        var joint = _joints.CreateMouseJoint(tether, target, id: TetherJoint);

        SharedJointSystem.LinearStiffness(5f, 2f, tetherPhysics.Mass, targetPhysics.Mass, out var stiffness, out var damping);
        joint.Stiffness = stiffness;
        joint.Damping = damping;
        joint.MaxForce = 10000f * targetPhysics.Mass;

        Dirty(tethered);
        Dirty(component);
    }

    private void StopTether(TetherGunComponent component)
    {
        if (component.Tethered == null)
            return;

        if (component.TetherEntity != null)
        {
            _joints.RemoveJoint(component.TetherEntity.Value, TetherJoint);
            QueueDel(component.TetherEntity.Value);
            component.TetherEntity = null;
        }

        if (TryComp<PhysicsComponent>(component.Tethered, out var targetPhysics))
        {
            var thrown = EnsureComp<ThrownItemComponent>(component.Tethered.Value);
            _thrown.LandComponent(component.Tethered.Value, thrown, targetPhysics);

            _physics.SetBodyStatus(targetPhysics, BodyStatus.OnGround);
            _physics.SetSleepingAllowed(component.Tethered.Value, targetPhysics, true);
            _physics.SetAngularDamping(targetPhysics, Comp<TetheredComponent>(component.Tethered.Value).OriginalAngularDamping);
        }

        RemCompDeferred<TetheredComponent>(component.Tethered.Value);
        component.Tethered = null;
        Dirty(component);
    }

    [Serializable, NetSerializable]
    protected sealed class RequestTetherMoveEvent : EntityEventArgs
    {
        public EntityCoordinates Coordinates;
    }
}
