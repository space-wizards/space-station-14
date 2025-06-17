using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Slippery;

public sealed class SlidingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlidingComponent, StoodEvent>(OnStand);
        SubscribeLocalEvent<SlidingComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<SlidingComponent, EndCollideEvent>(OnEndCollide);
    }

    /// <summary>
    ///     Remove the component when the entity stands up again.
    /// </summary>
    private void OnStand(EntityUid uid, SlidingComponent component, ref StoodEvent args)
    {
        RemComp<SlidingComponent>(uid);
    }

    /// <summary>
    ///     Sets friction to 0 if colliding with a SuperSlippery Entity.
    /// </summary>
    private void OnStartCollide(EntityUid uid, SlidingComponent component, ref StartCollideEvent args)
    {
        if (!TryComp<SlipperyComponent>(args.OtherEntity, out var slippery) || !slippery.SlipData.SuperSlippery)
            return;

        component.CollidingEntities.Add(args.OtherEntity);
        Dirty(uid, component);
    }

    /// <summary>
    ///     Set friction to normal when ending collision with a SuperSlippery entity.
    /// </summary>
    private void OnEndCollide(EntityUid uid, SlidingComponent component, ref EndCollideEvent args)
    {
        if (!component.CollidingEntities.Remove(args.OtherEntity))
            return;

        if (component.CollidingEntities.Count == 0)
            RemComp<SlidingComponent>(uid);

        Dirty(uid, component);
    }
}
