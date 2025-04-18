using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Slippery;

public sealed class SlidingSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifierSystem = default!;

    private readonly HashSet<EntityUid> _toUpdate = new();
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlidingComponent, StoodEvent>(OnStand);
        SubscribeLocalEvent<SlidingComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<SlidingComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<SlidingComponent, RefreshFrictionModifiersEvent>(OnRefreshFrictionModifiers);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var ent in _toUpdate)
        {
            _speedModifierSystem.RefreshFrictionModifiers(ent);
        }

        _toUpdate.Clear();
    }

    /// <summary>
    ///     Remove the component when the entity stands up again, and reset friction.
    /// </summary>
    private void OnStand(Entity<SlidingComponent> entity, ref StoodEvent args)
    {
        RemComp<SlidingComponent>(entity);
        if (HasComp<MovementSpeedModifierComponent>(entity))
            _toUpdate.Add(entity);
    }

    /// <summary>
    ///     Applies friction from a superSlippery Entity.
    /// </summary>
    private void OnStartCollide(Entity<SlidingComponent> entity, ref StartCollideEvent args)
    {
        if (!TryComp<SlipperyComponent>(args.OtherEntity, out var slippery))
            return;
        // Add colliding entity so it can be tracked.
        entity.Comp.CollidingEntities.Add(args.OtherEntity);
        // Set friction modifier for sliding to the friction modifier stored in the slipperyComponent.
        //RecalculateFriction(entity, slippery);
        entity.Comp.FrictionModifier = slippery.SlipData.SlipFriction;
        Dirty(entity, entity.Comp);
        // If this entity has a MovementSpeedModifierComponent we better edit the friction for that too.
        if (HasComp<MovementSpeedModifierComponent>(entity))
            _toUpdate.Add(entity);
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
            RemComp<SlidingComponent>(entity);
        }

        _toUpdate.Add(entity);
        Dirty(entity, component);
    }


    private void RecalculateFriction(Entity<SlidingComponent> entity, SlipperyComponent component)
    {

    }

    private void OnRefreshFrictionModifiers(Entity<SlidingComponent> entity, ref RefreshFrictionModifiersEvent args)
    {
        if (!TryComp<MovementSpeedModifierComponent>(entity, out var move))
            return;
        args.ModifyFriction(entity.Comp.FrictionModifier, entity.Comp.FrictionModifier);
        args.ModifyAcceleration(entity.Comp.FrictionModifier);
    }
}
