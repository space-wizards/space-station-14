using Content.Server.StatusEffectNew;
using Content.Shared.Bed.Sleep;
using Content.Shared.Drowsiness;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Drowsiness;

public sealed class DrowsinessSystem : SharedDrowsinessSystem
{

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectNewSystem _statusEffects = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DrowsinessStatusEffectComponent, ComponentStartup>(OnInit);
    }

    private void OnInit(EntityUid uid, DrowsinessStatusEffectComponent component, ComponentStartup args)
    {
        component.NextIncidentTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(component.TimeBetweenIncidents.X, component.TimeBetweenIncidents.Y));
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DrowsinessStatusEffectComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime < component.NextIncidentTime)
                continue;

            // Set the new time.
            component.NextIncidentTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(component.TimeBetweenIncidents.X, component.TimeBetweenIncidents.Y));

            // sleep duration
            var duration = TimeSpan.FromSeconds(_random.NextFloat(component.DurationOfIncident.X, component.DurationOfIncident.Y));

            // Make sure the sleep time doesn't cut into the time to next incident.
            component.NextIncidentTime += duration;

            _statusEffects.TryAddStatusEffect(uid, SleepingSystem.StatusEffectForcedSleeping, duration);
        }
    }
}
