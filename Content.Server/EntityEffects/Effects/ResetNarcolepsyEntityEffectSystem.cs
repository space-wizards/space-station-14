using Content.Server.Traits.Assorted;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class ResetNarcolepsyEntityEffectSystem : EntityEffectSystem<NarcolepsyComponent, ResetNarcolepsy>
{
    [Dependency] private readonly NarcolepsySystem _narcolepsy = default!;

    protected override void Effect(Entity<NarcolepsyComponent> entity, ref EntityEffectEvent<ResetNarcolepsy> args)
    {
        var timer = args.Effect.TimerReset * args.Scale;

        // TODO: Someone needs to bring Narcolepsy system up to modern standards.
        _narcolepsy.AdjustNarcolepsyTimer(entity, (int)timer.TotalSeconds);
    }
}
