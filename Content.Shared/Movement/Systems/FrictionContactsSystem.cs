using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Movement.Systems;

public sealed class FrictionContactsSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;

    // Comment copied from "original" SlowContactsSystem.cs (now SpeedModifierContactsSystem.cs)
    // TODO full-game-save 
    // Either these need to be processed before a map is saved, or slowed/slowing entities need to update on init.
    private HashSet<EntityUid> _toUpdate = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FrictionContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<FrictionContactsComponent, EndCollideEvent>(OnEntityExit);
        SubscribeLocalEvent<FrictionContactsComponent, ComponentShutdown>(OnShutdown);

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
    }

    private void OnEntityEnter(EntityUid uid, FrictionContactsComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (!HasComp(otherUid, typeof(MovementSpeedModifierComponent)))
            return;

        _toUpdate.Add(otherUid);
    }

    private void OnEntityExit(EntityUid uid, FrictionContactsComponent component, ref EndCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (!HasComp(otherUid, typeof(MovementSpeedModifierComponent)))
            return;

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

        foreach (var uid in _toUpdate)
        {
            ApplyFrictionChange(uid);
        }

        _toUpdate.Clear();
    }

    private void ApplyFrictionChange(EntityUid uid)
    {
        if (!EntityManager.TryGetComponent<PhysicsComponent>(uid, out var physicsComponent))
            return;

        if (!TryComp(uid, out MovementSpeedModifierComponent? speedModifier))
            return;

        FrictionContactsComponent? frictionComponent = TouchesFrictionContactsComponent(uid, physicsComponent);

        if (frictionComponent == null)
        {
            _speedModifierSystem.ChangeFriction(uid, MovementSpeedModifierComponent.DefaultFriction, null, MovementSpeedModifierComponent.DefaultAcceleration, speedModifier);
        }
        else
        {
            _speedModifierSystem.ChangeFriction(uid, frictionComponent.MobFriction, frictionComponent.MobFrictionNoInput, frictionComponent.MobAcceleration, speedModifier);
        }
    }

    private FrictionContactsComponent? TouchesFrictionContactsComponent(EntityUid uid, PhysicsComponent physicsComponent)
    {
        foreach (var ent in _physics.GetContactingEntities(uid, physicsComponent))
        {
            if (!TryComp(ent, out FrictionContactsComponent? frictionContacts))
                continue;

            return frictionContacts;
        }

        return null;
    }
}
