using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
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

    private const string TetherJoint = "tether";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TetherGunComponent, ActivateInWorldEvent>(OnTetherActivate);
        SubscribeLocalEvent<TetherGunComponent, AfterInteractEvent>(OnTetherRanged);
        SubscribeAllEvent<RequestTetherMoveEvent>(OnTetherMove);
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

    private void StartTether(EntityUid gunUid, TetherGunComponent component, EntityUid target, PhysicsComponent? targetPhysics = null)
    {
        if (!Resolve(target, ref targetPhysics))
            return;

        if (component.Tethered != null)
        {
            StopTether(component);
        }

        component.Tethered = target;
        var tethered = EnsureComp<TetheredComponent>(target);
        tethered.Tetherer = gunUid;

        var tether = Spawn("TetherEntity", Transform(target).MapPosition);
        component.TetherEntity = tether;

        _physics.WakeBody(tether);
        _physics.WakeBody(target, body: targetPhysics);

        var joint = _joints.CreateMouseJoint(tether, target, id: TetherJoint);

        SharedJointSystem.LinearStiffness(5f, 0.7f, Comp<PhysicsComponent>(tether).Mass, targetPhysics.Mass, out var stiffness, out var damping);
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

        RemCompDeferred<TetheredComponent>(component.Tethered.Value);
        component.Tethered = null;
        Dirty(component);
    }

    [Serializable, NetSerializable]
    protected sealed class RequestTetherMoveEvent : EntityEventArgs
    {
        public EntityUid Tethered;
        public EntityCoordinates Coordinates;
    }
}
