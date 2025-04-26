using System.Diagnostics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Slippery;

public sealed class SlidingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlidingComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<SlidingComponent, StoodEvent>(OnStand);
        SubscribeLocalEvent<SlidingComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<SlidingComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<SlidingComponent, RefreshFrictionModifiersEvent>(OnRefreshFrictionModifiers);
    }

    /// <summary>
    ///     When the component is first added, calculate the friction modifier we need.
    ///     Don't do this more than once to avoid mispredicts.
    /// </summary>
    private void OnComponentInit(Entity<SlidingComponent> entity, ref ComponentInit args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!CalculateSlidingModifier(entity))
            throw new Exception($"Entity by the name of {ToPrettyString(entity)} was given the Sliding Component despite not colliding with anything slippery");

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
        if (!TryComp<SlipperyComponent>(args.OtherEntity, out var slippery) || !slippery.AffectsSliding)
            return;

        if (!CalculateSlidingModifier(entity))
            throw new Exception($"Entity by the name of {ToPrettyString(entity)} was given the Sliding Component despite not colliding with anything slippery");

        _speedModifierSystem.RefreshFrictionModifiers(entity);
    }

    /// <summary>
    ///     Update friction when we stop colliding with a slippery entity
    /// </summary>
    private void OnEndCollide(Entity<SlidingComponent> entity, ref EndCollideEvent args)
    {
        if (!TryComp<SlipperyComponent>(args.OtherEntity, out var slippery) || !slippery.AffectsSliding)
            return;

        if (!CalculateSlidingModifier(entity, args.OtherEntity))
        {
            entity.Comp.FrictionModifier = 1;
            RemComp<SlidingComponent>(entity);
        }

        _speedModifierSystem.RefreshFrictionModifiers(entity);
    }

    /// <summary>
    ///     Gets contacting slippery entities and averages their friction modifiers.
    /// </summary>
    private bool CalculateSlidingModifier(Entity<SlidingComponent> entity, EntityUid? ignore = null)
    {
        var friction = 0.0f;
        var count = 0;

        foreach (var ent in _physics.GetContactingEntities(entity))
        {
            if (ent == ignore || !TryComp<SlipperyComponent>(ent, out var slippery) || !slippery.AffectsSliding)
                continue;

            friction += slippery.SlipData.SlipFriction;

            count++;
        }

        if (count == 0)
            return false;

        entity.Comp.FrictionModifier = friction / count;
        return true;
    }

    private void OnRefreshFrictionModifiers(Entity<SlidingComponent> entity, ref RefreshFrictionModifiersEvent args)
    {
        args.ModifyFriction(entity.Comp.FrictionModifier);
        args.ModifyAcceleration(entity.Comp.FrictionModifier);
    }
}
