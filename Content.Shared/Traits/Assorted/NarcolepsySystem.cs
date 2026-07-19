using Content.Shared.Bed.Sleep;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffectNew;
using Robust.Shared.GameStates;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This handles narcolepsy, causing the affected to fall asleep uncontrollably at a random interval.
/// </summary>
public sealed partial class NarcolepsySystem : EntitySystem
{
    private static readonly EntityTimerId IncidentTimer = new("incident");

    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NarcolepsyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NarcolepsyComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<NarcolepsyComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnMapInit(Entity<NarcolepsyComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextIncidentTime = _timing.CurTime + _random.Next(ent.Comp.MinTimeBetweenIncidents, ent.Comp.MaxTimeBetweenIncidents);
        DirtyField(ent, ent.Comp, nameof(ent.Comp.NextIncidentTime));
        Schedule(ent);
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
        Schedule((ent.Owner, ent.Comp));
    }

    private void OnHandleState(Entity<NarcolepsyComponent> ent, ref ComponentHandleState args)
    {
        Schedule(ent);
    }

    private void OnTimer(Entity<NarcolepsyComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != IncidentTimer)
            return;

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));

        var duration = ent.Comp.MinDurationOfIncident +
            (ent.Comp.MaxDurationOfIncident - ent.Comp.MinDurationOfIncident) * rand.NextDouble();

        ent.Comp.NextIncidentTime +=
            ent.Comp.MinTimeBetweenIncidents +
            (ent.Comp.MaxTimeBetweenIncidents - ent.Comp.MinTimeBetweenIncidents) * rand.NextDouble() + duration;
        DirtyField(ent, ent.Comp, nameof(ent.Comp.NextIncidentTime));
        Schedule(ent);

        _statusEffects.TryAddStatusEffectDuration(ent, SleepingSystem.StatusEffectForcedSleeping, duration);
    }

    private void Schedule(Entity<NarcolepsyComponent> ent)
    {
        _timers.SetTimerAt(ent, IncidentTimer, ent.Comp.NextIncidentTime);
    }
}
