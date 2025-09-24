using Content.Shared.Traits.Assorted;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class ResetNarcolepsyEntityEffectSystem : EntityEffectSystem<NarcolepsyComponent, ResetNarcolepsy>
{
    [Dependency] private readonly NarcolepsySystem _narcolepsy = default!;

    protected override void Effect(Entity<NarcolepsyComponent> entity, ref EntityEffectEvent<ResetNarcolepsy> args)
    {
        var timer = args.Effect.TimerReset * args.Scale;

        _narcolepsy.AdjustNarcolepsyTimer(entity.AsNullable(), timer);
    }
}


public sealed partial class ResetNarcolepsy : EntityEffectBase<ResetNarcolepsy>
{
    /// <summary>
    /// The time we set our narcolepsy timer to.
    /// </summary>
    [DataField("TimerReset")]
    public TimeSpan TimerReset = TimeSpan.FromSeconds(600);
}
