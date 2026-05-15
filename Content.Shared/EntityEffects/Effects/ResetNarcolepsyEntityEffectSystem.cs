using Content.Shared.Traits.Assorted;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Resets the narcolepsy timer on a given entity.
/// The new duration of the timer is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ResetNarcolepsyEntityEffectSystem : EntityEffectSystem<NarcolepsyComponent, ResetNarcolepsy>
{
    [Dependency] private readonly NarcolepsySystem _narcolepsy = default!;

    protected override void Effect(Entity<NarcolepsyComponent> entity, ref EntityEffectEvent<ResetNarcolepsy> args)
    {
        var timer = args.Effect.TimerReset * args.Scale;

        _narcolepsy.AdjustNarcolepsyTimer(entity.AsNullable(), timer);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ResetNarcolepsy : EntityEffectBase<ResetNarcolepsy>
{
    /// <summary>
    /// The time we set our narcolepsy timer to.
    /// </summary>
    [DataField("TimerReset")]
    public TimeSpan TimerReset = TimeSpan.FromSeconds(600);

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-reset-narcolepsy", ("chance", Probability));
}
