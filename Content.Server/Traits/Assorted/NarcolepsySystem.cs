using Content.Shared.Bed.Sleep;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Traits.Assorted;

/// <summary>
/// This handles narcolepsy, causing the affected to fall asleep uncontrollably at a random interval.
/// </summary>
public sealed class NarcolepsySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<NarcolepsyComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NarcolepsyComponent, GotStatusEffectRemovedEvent>(OnStatusEffectRemoved);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NarcolepsyComponent>();
        while (query.MoveNext(out var uid, out var narcolepsy))
        {
            if (narcolepsy.NextIncidentTime > _timing.CurTime)
                continue;

            SetNextIncidentTime((uid, narcolepsy));

            if (HasComp<SleepingComponent>(uid))
                return;

            var duration = _random.Next(narcolepsy.IncidentDuration.Min, narcolepsy.IncidentDuration.Max);
            _statusEffects.TryAddStatusEffectDuration(uid, SleepingSystem.StatusEffectForcedSleeping, duration);
            narcolepsy.NarcolepsyInducedSleep = true;
        }
    }

    private void OnStartup(Entity<NarcolepsyComponent> ent, ref ComponentStartup args)
    {
        SetNextIncidentTime(ent);
    }

    private void OnStatusEffectRemoved(Entity<NarcolepsyComponent> ent, ref GotStatusEffectRemovedEvent args)
    {
        if (!ent.Comp.NarcolepsyInducedSleep)
            return;

        SetNextIncidentTime(ent);
        ent.Comp.NarcolepsyInducedSleep = false;
    }

    private void SetNextIncidentTime(Entity<NarcolepsyComponent> ent)
    {
        ent.Comp.NextIncidentTime += _random.Next(ent.Comp.TimeBetweenIncidents.Min, ent.Comp.TimeBetweenIncidents.Max);
    }

    public void AdjustNarcolepsyTimer(EntityUid uid, TimeSpan timerReset, NarcolepsyComponent? narcolepsy = null)
    {
        if (!Resolve(uid, ref narcolepsy, false))
            return;

        narcolepsy.NextIncidentTime = timerReset;
    }
}
