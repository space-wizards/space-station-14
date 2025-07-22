using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Slippery;

public sealed class SlidingSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;

    private EntityQuery<SlipperyComponent> _slipperyQuery;

    public override void Initialize()
    {
        base.Initialize();

        _slipperyQuery = GetEntityQuery<SlipperyComponent>();

        SubscribeLocalEvent<SlidingComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SlidingComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<SlidingComponent, StoodEvent>(OnStand);
        SubscribeLocalEvent<SlidingComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<SlidingComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<SlidingComponent, RefreshFrictionModifiersEvent>(OnRefreshFrictionModifiers);
        SubscribeLocalEvent<SlidingComponent, ThrowerImpulseEvent>(OnThrowerImpulse);
        SubscribeLocalEvent<SlidingComponent, ShooterImpulseEvent>(ShooterImpulseEvent);
    }

    /// <summary>
    ///     When the component is first added, calculate the friction modifier we need.
    ///     Don't do this more than once to avoid mispredicts.
    /// </summary>
    private void OnComponentInit(Entity<SlidingComponent> entity, ref ComponentInit args)
    {
        if (CalculateSlidingModifier(entity))
            _speedModifierSystem.RefreshFrictionModifiers(entity);
    }

    /// <summary>
    ///     When the component is removed, refresh friction modifiers and set ours to 1 to avoid causing issues.
    /// </summary>
    private void OnComponentShutdown(Entity<SlidingComponent> entity, ref ComponentShutdown args)
    {
        entity.Comp.FrictionModifier = 1;
        _speedModifierSystem.RefreshFrictionModifiers(entity);
    }

    /// <summary>
    ///     Remove the component when the entity stands up again.
    /// </summary>
    private void OnStand(EntityUid uid, SlidingComponent component, ref StoodEvent args)
    {
        RemComp<SlidingComponent>(uid);
    }

    /// <summary>
    ///     Updates friction when we collide with a slippery entity
    /// </summary>
    private void OnStartCollide(Entity<SlidingComponent> entity, ref StartCollideEvent args)
    {
        if (!_slipperyQuery.TryComp(args.OtherEntity, out var slippery) || !slippery.AffectsSliding)
            return;

        CalculateSlidingModifier(entity);
        _speedModifierSystem.RefreshFrictionModifiers(entity);
    }

    /// <summary>
    ///     Update friction when we stop colliding with a slippery entity
    /// </summary>
    private void OnEndCollide(Entity<SlidingComponent> entity, ref EndCollideEvent args)
    {
        if (!_slipperyQuery.TryComp(args.OtherEntity, out var slippery) || !slippery.AffectsSliding)
            return;

        if (!CalculateSlidingModifier(entity, args.OtherEntity))
        {
            RemComp<SlidingComponent>(entity);
            return;
        }

        _speedModifierSystem.RefreshFrictionModifiers(entity);
    }

    /// <summary>
    ///     Gets contacting slippery entities and averages their friction modifiers.
    /// </summary>
    private bool CalculateSlidingModifier(Entity<SlidingComponent, PhysicsComponent?> entity, EntityUid? ignore = null)
    {
        if (!Resolve(entity, ref entity.Comp2, false))
            return false;

        var friction = 0.0f;
        var count = 0;
        entity.Comp1.Contacting.Clear();

        _physics.GetContactingEntities((entity, entity.Comp2), entity.Comp1.Contacting);

        foreach (var ent in entity.Comp1.Contacting)
        {
            if (ent == ignore || !_slipperyQuery.TryComp(ent, out var slippery) || !slippery.AffectsSliding)
                continue;

            friction += slippery.SlipData.SlipFriction;

            count++;
        }

        if (count > 0)
        {
            entity.Comp1.FrictionModifier = friction / count;
            Dirty(entity.Owner, entity.Comp1);
            return true;
        }

        return false;
    }

    private void OnRefreshFrictionModifiers(Entity<SlidingComponent> entity, ref RefreshFrictionModifiersEvent args)
    {
        args.ModifyFriction(entity.Comp.FrictionModifier);
        args.ModifyAcceleration(entity.Comp.FrictionModifier);
    }

    private void OnThrowerImpulse(Entity<SlidingComponent> entity, ref ThrowerImpulseEvent args)
    {
        args.Push = true;
    }

    private void ShooterImpulseEvent(Entity<SlidingComponent> entity, ref ShooterImpulseEvent args)
    {
        args.Push = true;
    }
}
