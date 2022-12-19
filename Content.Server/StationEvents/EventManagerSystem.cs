using System.Linq;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationEvents;

public sealed class EventManagerSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] public readonly GameTicker GameTicker = default!;

    private ISawmill _sawmill = default!;

    public bool EventsEnabled { get; private set; }
    private void SetEnabled(bool value) => EventsEnabled = value;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("events");

        _configurationManager.OnValueChanged(CCVars.EventsEnabled, SetEnabled, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _configurationManager.UnsubValueChanged(CCVars.EventsEnabled, SetEnabled);
    }

    /// <summary>
    /// Randomly runs a valid event.
    /// </summary>
    public string RunRandomEvent()
    {
        var randomEvent = PickRandomEvent();

        if (randomEvent == null
            || !_prototype.TryIndex<GameRulePrototype>(randomEvent.Id, out var proto))
        {
            var errStr = Loc.GetString("station-event-system-run-random-event-no-valid-events");
            _sawmill.Error(errStr);
            return errStr;
        }

        GameTicker.AddGameRule(proto);
        var str = Loc.GetString("station-event-system-run-event",("eventName", randomEvent.Id));
        _sawmill.Info(str);
        return str;
    }

    /// <summary>
    /// Randomly picks a valid event.
    /// </summary>
    public StationEventRuleConfiguration? PickRandomEvent()
    {
        var availableEvents = AvailableEvents();
        _sawmill.Info($"Picking from {availableEvents.Count} total available events");
        return FindEvent(availableEvents);
    }

    /// <summary>
    /// Pick a random event from the available events at this time, also considering their weightings.
    /// </summary>
    /// <returns></returns>
    private StationEventRuleConfiguration? FindEvent(List<StationEventRuleConfiguration> availableEvents)
    {
        if (availableEvents.Count == 0)
        {
            _sawmill.Warning("No events were available to run!");
            return null;
        }

        var sumOfWeights = 0;

        foreach (var stationEvent in availableEvents)
        {
            sumOfWeights += (int) stationEvent.Weight;
        }

        sumOfWeights = _random.Next(sumOfWeights);

        foreach (var stationEvent in availableEvents)
        {
            sumOfWeights -= (int) stationEvent.Weight;

            if (sumOfWeights <= 0)
            {
                return stationEvent;
            }
        }

        _sawmill.Error("Event was not found after weighted pick process!");
        return null;
    }

    /// <summary>
    /// Gets the events that have met their player count, time-until start, etc.
    /// </summary>
    /// <param name="ignoreEarliestStart"></param>
    /// <returns></returns>
    private List<StationEventRuleConfiguration> AvailableEvents(bool ignoreEarliestStart = false)
    {
        TimeSpan currentTime;
        var playerCount = _playerManager.PlayerCount;

        // playerCount does a lock so we'll just keep the variable here
        if (!ignoreEarliestStart)
        {
            currentTime = GameTicker.RoundDuration();
        }
        else
        {
            currentTime = TimeSpan.Zero;
        }

        var result = new List<StationEventRuleConfiguration>();

        foreach (var stationEvent in AllEvents())
        {
            if (CanRun(stationEvent, playerCount, currentTime))
            {
                _sawmill.Debug($"Adding event {stationEvent.Id} to possibilities");
                result.Add(stationEvent);
            }
        }

        return result;
    }

    private IEnumerable<StationEventRuleConfiguration> AllEvents()
    {
        return _prototype.EnumeratePrototypes<GameRulePrototype>()
            .Where(p => p.Configuration is StationEventRuleConfiguration)
            .Select(p => (StationEventRuleConfiguration) p.Configuration);
    }

    private int GetOccurrences(StationEventRuleConfiguration stationEvent)
    {
        return GameTicker.AllPreviousGameRules.Count(p => p.Item2.ID == stationEvent.Id);
    }

    public TimeSpan TimeSinceLastEvent(StationEventRuleConfiguration? stationEvent)
    {
        foreach (var (time, rule) in GameTicker.AllPreviousGameRules.Reverse())
        {
            if (rule.Configuration is not StationEventRuleConfiguration)
                continue;

            if (stationEvent == null || rule.ID == stationEvent.Id)
                return time;
        }

        return TimeSpan.Zero;
    }

    private bool CanRun(StationEventRuleConfiguration stationEvent, int playerCount, TimeSpan currentTime)
    {
        if (GameTicker.IsGameRuleStarted(stationEvent.Id))
            return false;

        if (stationEvent.MaxOccurrences.HasValue && GetOccurrences(stationEvent) >= stationEvent.MaxOccurrences.Value)
        {
            return false;
        }

        if (playerCount < stationEvent.MinimumPlayers)
        {
            return false;
        }

        if (currentTime != TimeSpan.Zero && currentTime.TotalMinutes < stationEvent.EarliestStart)
        {
            return false;
        }

        var lastRun = TimeSinceLastEvent(stationEvent);
        if (lastRun != TimeSpan.Zero && currentTime.TotalMinutes <
            stationEvent.ReoccurrenceDelay + lastRun.TotalMinutes)
        {
            return false;
        }

        return true;
    }
}
