using Content.Shared.Drowsiness;
using Content.Shared.Bed.Sleep;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;

namespace Content.Server.Drowsiness;

public sealed class DrowsinessSystem : SharedDrowsinessSystem
{
    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string SleepKey = "ForcedSleep"; // Same one used by N2O and other sleep chems.

    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DrowsinessComponent, ComponentStartup>(SetupDrowsiness);
    }

    private void SetupDrowsiness(EntityUid uid, DrowsinessComponent component, ComponentStartup args)
    {
        component.NextIncidentTime = _random.NextFloat(component.TimeBetweenIncidents.X, component.TimeBetweenIncidents.Y);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DrowsinessComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            component.NextIncidentTime -= frameTime;

            if (component.NextIncidentTime >= 0)
                continue;

            // Set the new time.
            component.NextIncidentTime += _random.NextFloat(component.TimeBetweenIncidents.X, component.TimeBetweenIncidents.Y);

            var duration = _random.NextFloat(component.DurationOfIncident.X, component.DurationOfIncident.Y);

            // Make sure the sleep time doesn't cut into the time to next incident.
            component.NextIncidentTime += duration;

            _statusEffects.TryAddStatusEffect<ForcedSleepingComponent>(uid, SleepKey, TimeSpan.FromSeconds(duration), false);
        }
    }
}
