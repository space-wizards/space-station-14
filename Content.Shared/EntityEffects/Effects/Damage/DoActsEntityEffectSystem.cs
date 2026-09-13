using Content.Shared.Destructible;

namespace Content.Shared.EntityEffects.Effects.Damage;

/// <summary>
/// Breaks or destroys the entity via its <see cref="DestructibleComponent"/> thresholds.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class DoActsEntityEffectSystem : EntityEffectSystem<TransformComponent, DoActs>
{
    [Dependency] private SharedDestructibleSystem _destructible = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<DoActs> args)
    {
        var acts = args.Effect.Acts;

        if ((acts & ThresholdActs.Breakage) != 0)
            _destructible.BreakEntity(entity);
        else if ((acts & ThresholdActs.Destruction) != 0)
            _destructible.DestroyEntity(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class DoActs : EntityEffectBase<DoActs>
{
    /// <summary>
    /// The destructible acts to perform, e.g. breakage or destruction.
    /// </summary>
    [DataField(required: true)]
    public ThresholdActs Acts { get; set; }

    /// <summary>
    /// Whether this effect performs the given act.
    /// </summary>
    public bool HasAct(ThresholdActs act)
    {
        return (Acts & act) != 0;
    }
}
