using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
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
    private void OnSlideAttempt(EntityUid entity, SlidingComponent component, ref TileFrictionEvent args)
    {
        args.Modifier = component.FrictionModifier;
        // This is purely a test to see if changing friction to MovementModifierComponent would help things,
        // Do not leave this code in it's not a good way of doing this
        if(!TryComp<MovementSpeedModifierComponent>(entity, out var comp))
            return;
        _speedModifierSystem.ChangeFriction(entity, component.FrictionModifier, component.FrictionModifier, component.FrictionModifier, comp);
    }

    /// <summary>
    ///     Remove the component when the entity stands up again.
    /// </summary>
    private void OnStand(EntityUid entity, SlidingComponent component, ref StoodEvent args)
    {
        RemComp<SlidingComponent>(entity);
        if (TryComp<MovementSpeedModifierComponent>(entity, out var comp))
        {
            _speedModifierSystem.ChangeFriction(entity, MovementSpeedModifierComponent.DefaultFriction, null, MovementSpeedModifierComponent.DefaultAcceleration, comp);
            Dirty(entity, comp);
        }
    }

    /// <summary>
    ///     Sets friction to 0 if colliding with a SuperSlippery Entity.
    /// </summary>
    private void OnStartCollide(EntityUid uid, SlidingComponent component, ref StartCollideEvent args)
    {
        if (!TryComp<SlipperyComponent>(args.OtherEntity, out var slippery) || !slippery.SlipData.SuperSlippery)
            return;

        component.CollidingEntities.Add(args.OtherEntity);
        component.FrictionModifier = 0;
        Dirty(uid, component);
    }

    /// <summary>
    ///     Set friction to normal when ending collision with a SuperSlippery entity.
    /// </summary>
    private void OnEndCollide(EntityUid entity, SlidingComponent component, ref EndCollideEvent args)
    {
        if (!component.CollidingEntities.Remove(args.OtherEntity))
            return;

        if (component.CollidingEntities.Count == 0)
        {
            component.FrictionModifier = SharedStunSystem.KnockDownModifier;
            if (TryComp<MovementSpeedModifierComponent>(entity, out var comp))
            {
                _speedModifierSystem.ChangeFriction(entity, MovementSpeedModifierComponent.DefaultFriction, null, MovementSpeedModifierComponent.DefaultAcceleration, comp);
                Dirty(entity, comp);
            }
        }

        Dirty(entity, component);
    }
}
