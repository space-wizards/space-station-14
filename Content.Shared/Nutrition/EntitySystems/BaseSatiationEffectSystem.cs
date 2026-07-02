using Content.Shared.Nutrition.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// This abstract system provides a convenient interface for implementing effects which react to changes in
/// <see cref="Satiation"/> thresholds.
/// </summary>
public abstract partial class BaseSatiationEffectSystem<TComp> : EntitySystem where TComp : Component
{
    [Dependency] protected SatiationSystem SatiationSystem = default!;
    [Dependency] protected EntityQuery<SatiationComponent> SatiationQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TComp, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TComp, SatiationThresholdChangedEvent>(OnThresholdChanged);
    }

    private void OnMapInit(Entity<TComp> entity, ref MapInitEvent args)
    {
        // Make sure we have a satiation component. Realistically, this just exists to cause test failures if an entity
        // with `TComp` doesn't have a `SatiationComponent`.
        var comp = EnsureComp<SatiationComponent>(entity);
        OnMapInit((entity, entity, comp), ref args);
    }

    protected virtual void OnMapInit(Entity<TComp, SatiationComponent> entity, ref MapInitEvent args) { }

    private void OnThresholdChanged(Entity<TComp> entity, ref SatiationThresholdChangedEvent args)
    {
        if (!SatiationQuery.TryComp(entity, out var comp))
            return;
        OnThresholdChanged((entity, entity, comp), ref args);
    }

    protected virtual void OnThresholdChanged(
        Entity<TComp, SatiationComponent> entity,
        ref SatiationThresholdChangedEvent args
    )
    {
    }
}

/// <summary>
/// A further extension to <see cref="BaseSatiationEffectSystem{T}"/>, this system makes it easy to implement regularly
/// applied effects based on satiation thresholds.
/// </summary>
/// <typeparam name="TComp"></typeparam>
public abstract partial class BaseContinuousSatiationEffectSystem<TComp> : BaseSatiationEffectSystem<TComp>
    where TComp : Component
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<TComp, SatiationComponent>();
        while (query.MoveNext(out var uid, out var component, out var satiation))
        {
            ref var time = ref GetContinuousEffectTime(component);
            if (_timing.CurTime < time)
                continue;

            // Update the next tick time.
            time += GetContinuousEffectFrequency(component);

            // Do effects
            OnContinuousEffect((uid, component, satiation));
        }
    }

    /// <summary>
    /// This function is called when the effects should be applied.
    /// </summary>
    protected abstract void OnContinuousEffect(Entity<TComp, SatiationComponent> entity);

    /// <summary>
    /// Retrieves the time between effect applications. Used to update the next effect time.
    /// </summary>
    protected abstract TimeSpan GetContinuousEffectFrequency(TComp comp);

    /// <summary>
    /// Gets the next effect time, for checking if we should apply effects, and for modifying the value to set the next
    /// effect time.
    /// </summary>
    protected abstract ref TimeSpan GetContinuousEffectTime(TComp comp);
}
