using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Movement.Systems;

public sealed class FrictionContactsSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;

    // Comment copied from "original" SlowContactsSystem.cs
    // TODO full-game-save
    // Either these need to be processed before a map is saved, or slowed/slowing entities need to update on init.

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FrictionContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<FrictionContactsComponent, EndCollideEvent>(OnEntityExit);
        // SubscribeLocalEvent<FrictionContactsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FrictionContactsComponent, RefreshMovementSpeedModifiersEvent>(OnMovementRefresh);

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
    }

    private void OnEntityEnter(EntityUid uid, FrictionContactsComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (!TryComp<MovementSpeedModifierComponent>(otherUid, out var moveComp))
            return;

        _speedModifierSystem.RefreshMovementSpeedModifiers(otherUid, moveComp);
    }

    private void OnEntityExit(EntityUid uid, FrictionContactsComponent component, ref EndCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (!TryComp<MovementSpeedModifierComponent>(otherUid, out var moveComp))
            return;

        _speedModifierSystem.RefreshMovementSpeedModifiers(otherUid, moveComp);
    }

    // private void OnShutdown(EntityUid uid, FrictionContactsComponent component, ComponentShutdown args)
    // {
    //     if (!TryComp(uid, out PhysicsComponent? phys))
    //         return;

    //     foreach (var otherUid in _physics.GetContactingEntities(uid, phys))
    //     {
    //         if (!HasComp<MovementSpeedModifierComponent>(otherUid, out var moveComp))
    //             return;

    //         _speedModifierSystem.RefreshMovementSpeedModifiers(otherUid, moveComp);
    //     }
    // }

    private void OnMovementRefresh(EntityUid uid, FrictionContactsComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        if (!TryComp<PhysicsComponent>(uid, out var physicsComponent))
            return;

        if (!TryComp(uid, out MovementSpeedModifierComponent? speedModifier))
            return;

        FrictionContactsComponent? frictionComponent = TouchesFrictionContactsComponent(uid, physicsComponent);

        if (frictionComponent == null)
            return;

        args.ModifyFriction(frictionComponent.MobFriction, frictionComponent.MobFrictionNoInput);
        args.ModifyAcceleration(frictionComponent.MobAcceleration);
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
