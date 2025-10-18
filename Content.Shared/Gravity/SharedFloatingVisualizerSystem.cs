using System.Numerics;
using Robust.Shared.Map;

namespace Content.Shared.Gravity;

/// <summary>
/// Handles offsetting a sprite when there is no gravity
/// </summary>
public abstract class SharedFloatingVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SharedGravitySystem _gravity = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FloatingVisualsComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<FloatingVisualsComponent, WeightlessnessChangedEvent>(OnWeightlessnessChanged);
    }

    /// <summary>
    /// Offsets a sprite with a linear interpolation animation
    /// </summary>
    public virtual void FloatAnimation(EntityUid uid, Vector2 offset, string animationKey, float animationTime, bool stop = false) { }

    protected bool CanFloat(Entity<FloatingVisualsComponent> entity)
    {
        entity.Comp.CanFloat = _gravity.IsWeightless(entity.Owner);
        Dirty(entity);
        return entity.Comp.CanFloat;
    }

    private void OnComponentStartup(Entity<FloatingVisualsComponent> entity, ref ComponentStartup args)
    {
        if (CanFloat(entity))
            FloatAnimation(entity, entity.Comp.Offset, entity.Comp.AnimationKey, entity.Comp.AnimationTime);
    }

    private void OnWeightlessnessChanged(Entity<FloatingVisualsComponent> entity, ref WeightlessnessChangedEvent args)
    {
        if (entity.Comp.CanFloat == args.Weightless)
            return;

        entity.Comp.CanFloat = CanFloat(entity);
        Dirty(entity);

        if (args.Weightless)
            FloatAnimation(entity, entity.Comp.Offset, entity.Comp.AnimationKey, entity.Comp.AnimationTime);
    }
}
