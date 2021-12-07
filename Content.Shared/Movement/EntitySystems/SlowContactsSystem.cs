using System;
using System.Collections.Generic;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Movement.EntitySystems;

public class SlowContactsSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;

    private readonly Dictionary<EntityUid, int> _statusCapableInContact = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlowContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<SlowContactsComponent, EndCollideEvent>(OnEntityExit);
        SubscribeLocalEvent<SlowsOnContactComponent, RefreshMovementSpeedModifiersEvent>(MovementSpeedCheck);
    }

    private void MovementSpeedCheck(EntityUid uid, SlowsOnContactComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!_statusCapableInContact.ContainsKey(uid) || _statusCapableInContact[uid] <= 0)
            return;

        if (!EntityManager.TryGetComponent<PhysicsComponent>(uid, out var physicsComponent))
            return;

        var walkSpeed = 1.0f;
        var sprintSpeed = 1.0f;

        foreach (var colliding in _physics.GetCollidingEntities(physicsComponent))
        {
            var ent = colliding.Owner;
            if (!EntityManager.TryGetComponent<SlowContactsComponent>(ent, out var slowContactsComponent))
                continue;

            walkSpeed = Math.Min(walkSpeed, slowContactsComponent.WalkSpeedModifier);
            sprintSpeed = Math.Min(sprintSpeed, slowContactsComponent.SprintSpeedModifier);
        }

        args.ModifySpeed(walkSpeed, sprintSpeed);
    }

    private void OnEntityExit(EntityUid uid, SlowContactsComponent component, EndCollideEvent args)
    {
        var otherUid = args.OtherFixture.Body.Owner;
        if (!EntityManager.HasComponent<MovementSpeedModifierComponent>(otherUid)
            || !EntityManager.HasComponent<SlowsOnContactComponent>(otherUid))
            return;
        if (!_statusCapableInContact.ContainsKey(otherUid))
            Logger.ErrorS("slowscontacts", $"The entity {otherUid} left a body ({uid}) it was never in.");
        _statusCapableInContact[otherUid]--;
        if (_statusCapableInContact[otherUid] == 0)
            EntityManager.RemoveComponent<SlowsOnContactComponent>(otherUid);
        _speedModifierSystem.RefreshMovementSpeedModifiers(otherUid);

    }

    private void OnEntityEnter(EntityUid uid, SlowContactsComponent component, StartCollideEvent args)
    {
        var otherUid = args.OtherFixture.Body.Owner;
        if (!EntityManager.HasComponent<MovementSpeedModifierComponent>(otherUid))
            return;
        if (!_statusCapableInContact.ContainsKey(otherUid))
            _statusCapableInContact[otherUid] = 0;
        _statusCapableInContact[otherUid]++;
        if (!EntityManager.HasComponent<SlowsOnContactComponent>(otherUid))
            EntityManager.AddComponent<SlowsOnContactComponent>(otherUid);
        _speedModifierSystem.RefreshMovementSpeedModifiers(otherUid);
    }
}
