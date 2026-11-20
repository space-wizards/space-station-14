using Content.Server.GameTicking;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Robust.Shared.Random;

namespace Content.Server.StationEvents;

/// <summary>
/// Handles ramping scheduling: starts other game rules more and more frequently as the round goes on.
/// </summary>
/// <seealso cref="RampingStationEventSchedulerComponent"/>
public sealed partial class RampingStationEventSchedulerSystem : GameRuleSystem<RampingStationEventSchedulerComponent>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EventManagerSystem _event = default!;
    [Dependency] private ServerGameTicker _gameTicker = default!;

    /// <summary>
    /// Returns the ChaosModifier which increases as round time increases to a point.
    /// </summary>
    public float GetChaosModifier(Entity<RampingStationEventSchedulerComponent> ent)
    {
        var roundTime = (float)_gameTicker.RoundDuration().TotalSeconds;
        if (roundTime > ent.Comp.EndTime)
            return ent.Comp.MaxChaos;

        return ent.Comp.MaxChaos / ent.Comp.EndTime * roundTime + ent.Comp.StartingChaos;
    }

    protected override void Started(Entity<RampingStationEventSchedulerComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        var scheduler = ent.Comp1;

        // Worlds shittiest probability distribution
        // Got a complaint? Send them to
        scheduler.MaxChaos = _random.NextFloat(scheduler.AverageChaos - scheduler.AverageChaos / 4, scheduler.AverageChaos + scheduler.AverageChaos / 4);
        // This is in minutes, so *60 for seconds (for the chaos calc)
        scheduler.EndTime = _random.NextFloat(scheduler.AverageEndTime - scheduler.AverageEndTime / 4, scheduler.AverageEndTime + scheduler.AverageEndTime / 4) * 60f;
        scheduler.StartingChaos = scheduler.MaxChaos / 10;

        PickNextEventTime(ent);
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

        PickNextEventTime((entityUid, component));
        _event.RunRandomEvent(component.ScheduledGameRules);
    }

    /// <summary>
    /// Sets the timing of the next event addition.
    /// </summary>
    private void PickNextEventTime(Entity<RampingStationEventSchedulerComponent> ent)
    {
        var mod = GetChaosModifier(ent);

        // 4-12 minutes baseline. Will get faster over time as the chaos mod increases.
        ent.Comp.TimeUntilNextEvent = _random.NextFloat(240f / mod, 720f / mod);
    }
}
