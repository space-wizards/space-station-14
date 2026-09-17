using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Robust.Shared.Random;

namespace Content.Server.StationEvents;

public sealed partial class RampingStationEventSchedulerSystem : GameRuleSystem<RampingStationEventSchedulerComponent>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EventManagerSystem _event = default!;
    [Dependency] private ServerGameTicker _gameTicker = default!;

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

    // TODO: GO THROUGH EVERY SINGLE GAME RULE AND JUST CLEAN THIS STUFF UP!!!
    protected override void ActiveTick(EntityUid entityUid, RampingStationEventSchedulerComponent component, GameRuleComponent gameRuleComponent, float frameTime)
    {
        if (!_event.EventsEnabled)
            return;

        if (component.TimeUntilNextEvent > 0f)
        {
            component.TimeUntilNextEvent -= frameTime;
            return;
        }

        PickNextEventTime(entityUid, component);
        _event.RunRandomEvent(component.ScheduledGameRules);
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
