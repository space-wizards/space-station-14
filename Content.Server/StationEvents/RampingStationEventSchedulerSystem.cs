using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents;

public sealed class RampingStationEventSchedulerSystem : GameRuleSystem<RampingStationEventSchedulerComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EventManagerSystem _event = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    /// <summary>
    /// Returns the ChaosModifier which increases as round time increases to a point.
    /// </summary>
    public float GetChaosModifier(EntityUid uid, RampingStationEventSchedulerComponent component)
    {
        var roundTime = (float) _gameTicker.RoundDuration().TotalSeconds;
        if (roundTime > component.EndTime)
            return component.MaxChaos;

        return component.MaxChaos / component.EndTime * roundTime + component.StartingChaos;
    }

    protected override void Started(EntityUid uid, RampingStationEventSchedulerComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        // Worlds shittiest probability distribution
        // Got a complaint? Send them to
        component.MaxChaos = _random.NextFloat(component.AverageChaos - component.AverageChaos / 4, component.AverageChaos + component.AverageChaos / 4);
        // This is in minutes, so *60 for seconds (for the chaos calc)
        component.EndTime = _random.NextFloat(component.AverageEndTime - component.AverageEndTime / 4, component.AverageEndTime + component.AverageEndTime / 4) * 60f;
        component.StartingChaos = component.MaxChaos / 10;

        PickNextEventTime(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_event.EventsEnabled)
            return;

        var query = EntityQueryEnumerator<RampingStationEventSchedulerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var scheduler, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            if (scheduler.TimeUntilNextEvent > 0f)
            {
                scheduler.TimeUntilNextEvent -= frameTime;
                continue;
            }

            PickNextEventTime(uid, scheduler);
            _event.RunRandomEvent(scheduler.ScheduledGameRules);
        }
    }

    /// <summary>
    /// Sets the timing of the next event addition.
    /// </summary>
    private void PickNextEventTime(EntityUid uid, RampingStationEventSchedulerComponent component)
    {
        var mod = GetChaosModifier(uid, component);

        // 4-12 minutes baseline. Will get faster over time as the chaos mod increases.
        component.TimeUntilNextEvent = _random.NextFloat(240f / mod, 720f / mod);
    }
}
