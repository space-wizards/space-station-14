using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Movement.Systems;

public sealed class FrictionContactsSystem : EntitySystem
{
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;

    // Comment copied from "original" SlowContactsSystem.cs (now SpeedModifierContactsSystem.cs)
    // TODO full-game-save
    // Either these need to be processed before a map is saved, or slowed/slowing entities need to update on init.
    private readonly HashSet<EntityUid> _toUpdate = new();
    private readonly HashSet<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FrictionContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<FrictionContactsComponent, EndCollideEvent>(OnEntityExit);
        SubscribeLocalEvent<FrictionModifiedByContactComponent, RefreshFrictionModifiersEvent>(OnRefreshFrictionModifiers);
        SubscribeLocalEvent<FrictionContactsComponent, ComponentShutdown>(OnShutdown);

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _toRemove.Clear();

        foreach (var ent in _toUpdate)
        {
            _speedModifierSystem.RefreshFrictionModifiers(ent);
        }

        foreach (var ent in _toRemove)
        {
            RemComp<FrictionModifiedByContactComponent>(ent);
        }

        _toUpdate.Clear();
    }

    public void ChangeFrictionModifiers(EntityUid uid, float friction, FrictionContactsComponent? component = null)
    {
        ChangeFrictionModifiers(uid, friction, null, null, component);
    }

    public void ChangeFrictionModifiers(EntityUid uid, float mobFriction, float? mobFrictionNoInput, float? acceleration, FrictionContactsComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.MobFriction = mobFriction;
        component.MobFrictionNoInput = mobFrictionNoInput;
        if (acceleration.HasValue)
            component.MobAcceleration = acceleration.Value;
        Dirty(uid, component);
        _toUpdate.UnionWith(_physics.GetContactingEntities(uid));
    }

    private void OnShutdown(EntityUid uid, FrictionContactsComponent component, ComponentShutdown args)
    {
        if (!TryComp(uid, out PhysicsComponent? phys))
            return;

        // Note that the entity may not be getting deleted here. E.g., glue puddles.
        _toUpdate.UnionWith(_physics.GetContactingEntities(uid, phys));
    }

    private void OnRefreshFrictionModifiers(Entity<FrictionModifiedByContactComponent> entity, ref RefreshFrictionModifiersEvent args)
    {
        if (!EntityManager.TryGetComponent<PhysicsComponent>(entity, out var physicsComponent))
            return;

        var friction = 0.0f;
        var frictionNoInput = 0.0f;
        var acceleration = 0.0f;

        var isAirborne = physicsComponent.BodyStatus == BodyStatus.InAir || _gravity.IsWeightless(entity, physicsComponent);

        var remove = true;
        var entries = 0;
        foreach (var ent in _physics.GetContactingEntities(entity, physicsComponent))
        {
            if (!TryComp<FrictionContactsComponent>(ent, out var contacts))
                continue;

            // Entities that are airborne should not be affected by contact slowdowns that are specified to not affect airborne entities.
            if (isAirborne && !contacts.AffectAirborne)
                continue;

            friction += contacts.MobFriction;
            frictionNoInput += contacts.MobFrictionNoInput ?? contacts.MobFriction;
            acceleration += contacts.MobAcceleration;
            remove = false;
            entries++;
        }

        if (entries > 0)
        {
            if (!MathHelper.CloseTo(friction, entries) || !MathHelper.CloseTo(frictionNoInput, entries))
            {
                friction /= entries;
                frictionNoInput /= entries;
                args.ModifyFriction(friction, frictionNoInput);
            }

            if (!MathHelper.CloseTo(acceleration, entries))
            {
                acceleration /= entries;
                args.ModifyAcceleration(acceleration);
            }
        }

        // no longer colliding with anything
        if (remove)
            _toRemove.Add(entity);
    }

    private void OnEntityExit(EntityUid uid, FrictionContactsComponent component, ref EndCollideEvent args)
    {
        var otherUid = args.OtherEntity;
        _toUpdate.Add(otherUid);
    }

    private void OnEntityEnter(EntityUid uid, FrictionContactsComponent component, ref StartCollideEvent args)
    {
        AddModifiedEntity(args.OtherEntity);
    }

    public void AddModifiedEntity(EntityUid uid)
    {
        if (!HasComp<MovementSpeedModifierComponent>(uid))
            return;

        EnsureComp<FrictionModifiedByContactComponent>(uid);
        _toUpdate.Add(uid);
    }
}
