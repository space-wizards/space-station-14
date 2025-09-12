using Content.Shared.Bed.Sleep;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This handles narcolepsy, causing the affected to fall asleep uncontrollably at a random interval.
/// </summary>
public sealed class NarcolepsySystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NarcolepsyComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(Entity<NarcolepsyComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.NextIncidentTime = _timing.CurTime + _random.Next(ent.Comp.MinTimeBetweenIncidents, ent.Comp.MaxTimeBetweenIncidents);
        DirtyField(ent, ent.Comp, nameof(ent.Comp.NextIncidentTime));
    }

    /// <summary>
    /// Changes the time until the next incident.
    /// </summary>
    public void AdjustNarcolepsyTimer(Entity<NarcolepsyComponent?> ent, TimeSpan time)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.NextIncidentTime = _timing.CurTime + time;
        DirtyField(ent, ent.Comp, nameof(ent.Comp.NextIncidentTime));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NarcolepsyComponent>();

        while (query.MoveNext(out var uid, out var narcolepsy))
        {
            if (narcolepsy.NextIncidentTime > _timing.CurTime)
                continue;

            var duration = _random.Next(narcolepsy.MinDurationOfIncident, narcolepsy.MaxDurationOfIncident);

            // Set the new time.
            narcolepsy.NextIncidentTime +=
                _random.Next(narcolepsy.MinTimeBetweenIncidents, narcolepsy.MaxTimeBetweenIncidents) + duration;
            DirtyField(uid, narcolepsy, nameof(narcolepsy.NextIncidentTime));

            _statusEffects.TryAddStatusEffectDuration(uid, SleepingSystem.StatusEffectForcedSleeping, duration);
        }
    }
}
