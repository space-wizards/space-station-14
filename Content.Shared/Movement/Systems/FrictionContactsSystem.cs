using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Movement.Systems;

// SlowContactsSystem ripoff
public sealed class FrictionContactsSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;

    private HashSet<EntityUid> _toUpdate = new();
    private HashSet<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FrictionContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<FrictionContactsComponent, EndCollideEvent>(OnEntityExit);
        SubscribeLocalEvent<FrictionByContactComponent, RefreshMovementSpeedModifiersEvent>(MovementSpeedCheck);
        SubscribeLocalEvent<FrictionContactsComponent, ComponentShutdown>(OnShutdown);

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
    }

    private void OnEntityEnter(EntityUid uid, FrictionContactsComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (!HasComp(otherUid, typeof(MovementSpeedModifierComponent)))
            return;

        EnsureComp<FrictionByContactComponent>(otherUid);
        _toUpdate.Add(otherUid);
    }

    private void OnEntityExit(EntityUid uid, FrictionContactsComponent component, ref EndCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        _toUpdate.Add(otherUid);
    }

    private void OnShutdown(EntityUid uid, FrictionContactsComponent component, ComponentShutdown args)
    {
        if (!TryComp(uid, out PhysicsComponent? phys))
            return;

        _toUpdate.UnionWith(_physics.GetContactingEntities(uid, phys));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _toRemove.Clear();

        foreach (var ent in _toUpdate)
        {
            _speedModifierSystem.RefreshMovementSpeedModifiers(ent);
        }

        foreach (var ent in _toRemove)
        {
            RemComp<FrictionByContactComponent>(ent);
        }

        _toUpdate.Clear();
    }

    private void MovementSpeedCheck(EntityUid uid, FrictionByContactComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!EntityManager.TryGetComponent<PhysicsComponent>(uid, out var physicsComponent))
            return;

        float friction = MovementSpeedModifierComponent.DefaultFriction;
        float? frictionNoInput = null;
        float acceleration = MovementSpeedModifierComponent.DefaultAcceleration;

        bool remove = true;
        foreach (var ent in _physics.GetContactingEntities(uid, physicsComponent))
        {
            if (!TryComp<FrictionContactsComponent>(ent, out var frictionComponent))
                continue;

            friction = frictionComponent.MobFriction;
            frictionNoInput = frictionComponent.MobFrictionNoInput;
            acceleration = frictionComponent.MobAcceleration;

            remove = false;
        }

        args.ChangeFriction(friction, frictionNoInput, acceleration);

        if (remove)
        {
            _toRemove.Add(uid);
        }
    }
}
