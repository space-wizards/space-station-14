using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Slippery;

public sealed class SlidingSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlidingComponent, TileFrictionEvent>(OnSlideAttempt);
        SubscribeLocalEvent<SlidingComponent, StoodEvent>(OnStand);
        SubscribeLocalEvent<SlidingComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<SlidingComponent, EndCollideEvent>(OnEndCollide);
    }

    /// <summary>
    ///     Modify the friction by the frictionModifier stored on the component.
    /// </summary>
    private void OnSlideAttempt(Entity<SlidingComponent> entity, ref TileFrictionEvent args)
    {
        args.Modifier = entity.Comp.FrictionModifier;
        if (!TryComp<MovementSpeedModifierComponent>(entity, out var component))
            return;
        _speedModifierSystem.ChangeFriction(entity,
            entity.Comp.FrictionModifier,
            entity.Comp.FrictionModifier,
            entity.Comp.FrictionModifier,
            component);
    }

    /// <summary>
    ///     Remove the component when the entity stands up again, and reset friction.
    /// </summary>
    private void OnStand(EntityUid entity, SlidingComponent component, ref StoodEvent args)
    {
        RemComp<SlidingComponent>(entity);
        if (TryComp<MovementSpeedModifierComponent>(entity, out var comp))
            _speedModifierSystem.ChangeFriction(entity, MovementSpeedModifierComponent.DefaultFriction, null, MovementSpeedModifierComponent.DefaultAcceleration, comp);
    }

    /// <summary>
    ///     Applies friction from a superSlippery Entity.
    /// </summary>
    private void OnStartCollide(Entity<SlidingComponent> entity, ref StartCollideEvent args)
    {
        if (!TryComp<SlipperyComponent>(args.OtherEntity, out var slippery) || !slippery.SlipData.SuperSlippery)
            return;
        // Add colliding entity so it can be tracked.
        entity.Comp.CollidingEntities.Add(args.OtherEntity);
        // Set friction modifier for sliding to the friction modifier stored in the slipperyComponent.
        entity.Comp.FrictionModifier = slippery.SlipData.SlipFriction;
        Dirty(entity, entity.Comp);
        // If this entity has a MovementSpeedModifierComponent we better edit the friction for that too.
        if (TryComp<MovementSpeedModifierComponent>(entity, out var comp))
            _speedModifierSystem.ChangeFriction(entity, entity.Comp.FrictionModifier, entity.Comp.FrictionModifier, entity.Comp.FrictionModifier, comp);
    }

    /// <summary>
    ///     Set friction to normal when ending collision with a SuperSlippery entity.
    ///     Remove SlidingComponent if entity is no longer sliding.
    /// </summary>
    private void OnEndCollide(EntityUid entity, SlidingComponent component, ref EndCollideEvent args)
    {
        // Remove entity we're no longer colliding with from being tracked or return
        if (!component.CollidingEntities.Remove(args.OtherEntity))
            return;

        // If we aren't colliding with any superSlippery Entities, stop sliding
        if (component.CollidingEntities.Count == 0)
        {
            if (TryComp<MovementSpeedModifierComponent>(entity, out var comp))
                _speedModifierSystem.ChangeFriction(entity, MovementSpeedModifierComponent.DefaultFriction, null, MovementSpeedModifierComponent.DefaultAcceleration, comp);
            RemComp<SlidingComponent>(entity);
        }

        Dirty(entity, component);
    }
}
