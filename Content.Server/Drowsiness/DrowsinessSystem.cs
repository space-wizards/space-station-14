using Content.Shared.Bed.Sleep;
using Content.Shared.Drowsiness;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Drowsiness;

public sealed partial class DrowsinessSystem : SharedDrowsinessSystem
{
    private static readonly EntityTimerId IncidentTimer = new("incident");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DrowsinessStatusEffectComponent, StatusEffectAppliedEvent>(OnEffectApplied);
        SubscribeLocalEvent<DrowsinessStatusEffectComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnEffectApplied(Entity<DrowsinessStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.NextIncidentTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(ent.Comp.TimeBetweenIncidents.X, ent.Comp.TimeBetweenIncidents.Y));
        _timers.SetTimerAt(ent, IncidentTimer, ent.Comp.NextIncidentTime);
    }

    private void OnTimer(Entity<DrowsinessStatusEffectComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != IncidentTimer || !TryComp<StatusEffectComponent>(ent, out var statusEffect))
            return;

        var duration = TimeSpan.FromSeconds(_random.NextFloat(ent.Comp.DurationOfIncident.X, ent.Comp.DurationOfIncident.Y));
        ent.Comp.NextIncidentTime = args.FiredAt +
            TimeSpan.FromSeconds(_random.NextFloat(ent.Comp.TimeBetweenIncidents.X, ent.Comp.TimeBetweenIncidents.Y)) +
            duration;
        _timers.SetTimerAt(ent, IncidentTimer, ent.Comp.NextIncidentTime);

        if (statusEffect.AppliedTo is { } target)
            _statusEffects.TryAddStatusEffectDuration(target, SleepingSystem.StatusEffectForcedSleeping, duration);
    }
}
