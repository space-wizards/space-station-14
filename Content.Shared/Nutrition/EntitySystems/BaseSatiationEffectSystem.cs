using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// This abstract system provides a convenient interface for implementing effects which react to changes in
/// <see cref="Satiation"/> thresholds.
/// </summary>
/// <example>
/// <see cref="SatiationDamageSystem"/> uses this to maintain a damage descriptor which changes depending on the entity's
/// satiation values. When the satiation values change, the damage descriptor is automatically updated by this system.
/// </example>
/// <typeparam name="TComp">The type of component this system interacts with</typeparam>
/// <typeparam name="T">
/// The type of value this system maintains. It must be contained within a <see cref="SatiationThresholds{T}"/>
/// <see cref="GetThresholds">accessible via <typeparamref name="TComp">TComp</typeparamref></see>.
/// </typeparam>
/// <remarks>Note that this <b>is not</b> related to <see cref="EntityEffects"/></remarks>
public abstract partial class BaseSatiationEffectSystem<TComp, T> : EntitySystem where TComp : Component
{
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private EntityQuery<SatiationComponent> _satiationQuery;
    [Dependency] private IGameTiming _timing = default!;

    /// <summary>
    /// How to access <see cref="SatiationThresholds{T}"/> via a <typeparamref name="TComp"/>.
    /// </summary>
    protected abstract Dictionary<ProtoId<SatiationTypePrototype>, SatiationThresholds<T>> GetThresholds(TComp comp);

    /// <summary>
    /// The default <typeparamref name="T"/> value to set our maintained value to in the case that our current satiation
    /// is outside of <see cref="GetThresholds"/>.
    /// </summary>
    protected abstract T DefaultValue();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TComp, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TComp, SatiationUpdateEvent>(OnSatiationUpdate);
    }

    /// <inheritdoc/>
    // Updates maintained values when reaching the projected threshold time.
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<TComp, SatiationComponent>();
        while (query.MoveNext(out var ent, out var comp, out var satiation))
        {
            foreach (var (type, thresholds) in GetThresholds(comp))
            {
                if (_timing.CurTime < thresholds.ProjectedThresholdChangeTime)
                    continue;

                UpdateSatiation((ent, comp), satiation, type);
            }
        }
    }

    [MustCallBase]
    protected virtual void OnMapInit(Entity<TComp> entity, ref MapInitEvent args)
    {
        // Make sure we have a satiation component. Realistically, this just exists to cause test failures if an entity
        // with `TComp` doesn't have a `SatiationComponent`.
        var comp = EnsureComp<SatiationComponent>(entity);
        foreach (var type in GetThresholds(entity.Comp).Keys)
        {
            UpdateSatiation(entity, comp, type);
        }
    }

    // If a satiation value is changed directly, we react and update our maintained value.
    [MustCallBase]
    protected void OnSatiationUpdate(Entity<TComp> entity, ref SatiationUpdateEvent args)
    {
        if (!_satiationQuery.TryComp(entity, out var comp))
            return;

        UpdateSatiation(entity, comp, args.Type);
    }

    /// <summary>
    /// Updates the maintained <typeparamref name="T"/> based on the entity's satiation.
    /// </summary>
    private void UpdateSatiation(Entity<TComp> entity, SatiationComponent comp, ProtoId<SatiationTypePrototype> type)
    {
        // Get the current satiation value...
        if (!GetThresholds(entity.Comp).TryGetValue(type, out var thresholds))
            return;

        // ... and then use it to get the appropriate threshold T value.
        if (_satiation.TryGetValueByThreshold(
                (entity, comp),
                type,
                thresholds.Thresholds,
                out var result,
                out var nextLowerThreshold))
        {
            thresholds.Current = result ?? DefaultValue();

            // Predict when our satiation will decay to the next threshold down.
            thresholds.ProjectedThresholdChangeTime = nextLowerThreshold is { } lower
                ? _satiation.GetTimeToDecay((entity, comp), type, lower)
                : null;
        }
        else
        {
            thresholds.Current = DefaultValue();
            thresholds.ProjectedThresholdChangeTime = null;
        }

        Dirty(entity);

        AfterSatiationUpdate(entity);
    }

    /// <summary>
    /// This function is called after <see cref="UpdateSatiation"/> completes its work maintaining <typeparamref name="T"/>.
    /// </summary>
    protected virtual void AfterSatiationUpdate(Entity<TComp> entity) { }
}
