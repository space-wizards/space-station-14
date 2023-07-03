using Robust.Shared.Physics.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Content.Shared.Silicons.Bots;
using Content.Shared.Spider;

namespace Content.Shared.Movement.Systems;


public sealed class FastContactsSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;

    // TODO full-game-save
    // Either these need to be processed before a map is saved, or slowed/slowing entities need to update on init.
    private HashSet<EntityUid> _toUpdate = new();
    private HashSet<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FastContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<FastContactsComponent, EndCollideEvent>(OnEntityExit);
        SubscribeLocalEvent<FasterOnContactComponent, RefreshMovementSpeedModifiersEvent>(MovementSpeedCheck);
        SubscribeLocalEvent<FastContactsComponent, ComponentShutdown>(OnShutdown);

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
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
            RemComp<FasterOnContactComponent>(ent);
        }

        _toUpdate.Clear();
    }

    public void ChangeModifiers(EntityUid uid, float speed, FastContactsComponent? component = null)
    {
        ChangeModifiers(uid, speed, speed, component);
    }

    public void ChangeModifiers(EntityUid uid, float walkSpeed, float sprintSpeed, FastContactsComponent? component = null)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }
        component.WalkSpeedModifier = walkSpeed;
        component.SprintSpeedModifier = sprintSpeed;
        Dirty(component);
        _toUpdate.UnionWith(_physics.GetContactingEntities(uid));
    }

    private void OnShutdown(EntityUid uid, FastContactsComponent component, ComponentShutdown args)
    {
        if (!TryComp(uid, out PhysicsComponent? phys))
            return;

        // Note that the entity may not be getting deleted here. E.g., glue puddles.
        _toUpdate.UnionWith(_physics.GetContactingEntities(uid, phys));
    }

    private void MovementSpeedCheck(EntityUid uid, FasterOnContactComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (!EntityManager.TryGetComponent<PhysicsComponent>(uid, out var physicsComponent))
            return;
        if (!HasComp<SpiderComponent>(uid))
            return;

        var walkSpeed = 1.0f;
        var sprintSpeed = 1.0f;

        bool remove = true;
        foreach (var ent in _physics.GetContactingEntities(uid, physicsComponent))
        {
            if (!TryComp<FastContactsComponent>(ent, out var fastContactsComponent))
                continue;

            walkSpeed = Math.Max(walkSpeed, fastContactsComponent.WalkSpeedModifier);
            sprintSpeed = Math.Max(sprintSpeed, fastContactsComponent.SprintSpeedModifier);
            remove = false;
        }

        args.ModifySpeed(walkSpeed, sprintSpeed);

        // no longer colliding with anything
        if (remove)
            _toRemove.Add(uid);
    }

    private void OnEntityExit(EntityUid uid, FastContactsComponent component, ref EndCollideEvent args)
    {
        var otherUid = args.OtherEntity;
        _toUpdate.Add(otherUid);
    }

    private void OnEntityEnter(EntityUid uid, FastContactsComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherEntity;
        if (!HasComp<MovementSpeedModifierComponent>(otherUid))
            return;

        EnsureComp<FasterOnContactComponent>(otherUid);
        _toUpdate.Add(otherUid);
    }
}

