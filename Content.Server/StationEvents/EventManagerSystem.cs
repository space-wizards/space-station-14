using System.Linq;
using Content.Server.GameTicking;
using Content.Server.StationEvents.Components;
using Content.Shared.CCVar;
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

        SubscribeLocalEvent<StationEventComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnUnpaused(EntityUid uid, StationEventComponent component, ref EntityUnpausedEvent args)
    {
        component.StartTime += args.PausedTime;
        if (component.EndTime != null)
            component.EndTime = component.EndTime.Value + args.PausedTime;
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

        if (randomEvent == null)
        {
            var errStr = Loc.GetString("station-event-system-run-random-event-no-valid-events");
            _sawmill.Error(errStr);
            return errStr;
        }

        var ent = GameTicker.AddGameRule(randomEvent);
        var str = Loc.GetString("station-event-system-run-event",("eventName", ToPrettyString(ent)));
        _sawmill.Info(str);
        return str;
    }

    /// <summary>
    /// Randomly picks a valid event.
    /// </summary>
    public string? PickRandomEvent()
    {
        var availableEvents = AvailableEvents();
        _sawmill.Info($"Picking from {availableEvents.Count} total available events");
        return FindEvent(availableEvents);
    }

    /// <summary>
    /// Pick a random event from the available events at this time, also considering their weightings.
    /// </summary>
    /// <returns></returns>
    private string? FindEvent(Dictionary<EntityPrototype, StationEventComponent> availableEvents)
    {
        if (availableEvents.Count == 0)
        {
            _sawmill.Warning("No events were available to run!");
            return null;
        }

        var sumOfWeights = 0;

        foreach (var stationEvent in availableEvents.Values)
        {
            sumOfWeights += (int) stationEvent.Weight;
        }

        sumOfWeights = _random.Next(sumOfWeights);

        foreach (var (proto, stationEvent) in availableEvents)
        {
            sumOfWeights -= (int) stationEvent.Weight;

            if (sumOfWeights <= 0)
            {
                return proto.ID;
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
    private Dictionary<EntityPrototype, StationEventComponent> AvailableEvents(bool ignoreEarliestStart = false)
    {
        var playerCount = _playerManager.PlayerCount;

        // playerCount does a lock so we'll just keep the variable here
        var currentTime = !ignoreEarliestStart
            ? GameTicker.RoundDuration()
            : TimeSpan.Zero;

        var result = new Dictionary<EntityPrototype, StationEventComponent>();

        foreach (var (proto, stationEvent) in AllEvents())
        {
            if (CanRun(proto, stationEvent, playerCount, currentTime))
            {
                _sawmill.Debug($"Adding event {proto.ID} to possibilities");
                result.Add(proto, stationEvent);
            }
        }

        return result;
    }

    public Dictionary<EntityPrototype, StationEventComponent> AllEvents()
    {
        var allEvents = new Dictionary<EntityPrototype, StationEventComponent>();
        foreach (var prototype in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.Abstract)
                continue;

            if (!prototype.TryGetComponent<StationEventComponent>(out var stationEvent))
                continue;

            allEvents.Add(prototype, stationEvent);
        }

        return allEvents;
    }

    private int GetOccurrences(EntityPrototype stationEvent)
    {
        return GetOccurrences(stationEvent.ID);
    }

    private int GetOccurrences(string stationEvent)
    {
        return GameTicker.AllPreviousGameRules.Count(p => p.Item2 == stationEvent);
    }

    public TimeSpan TimeSinceLastEvent(EntityPrototype stationEvent)
    {
        foreach (var (time, rule) in GameTicker.AllPreviousGameRules.Reverse())
        {
            if (rule == stationEvent.ID)
                return time;
        }

        return TimeSpan.Zero;
    }

    private bool CanRun(EntityPrototype prototype, StationEventComponent stationEvent, int playerCount, TimeSpan currentTime)
    {
        if (GameTicker.IsGameRuleActive(prototype.ID))
            return false;

        if (stationEvent.MaxOccurrences.HasValue && GetOccurrences(prototype) >= stationEvent.MaxOccurrences.Value)
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

        var lastRun = TimeSinceLastEvent(prototype);
        if (lastRun != TimeSpan.Zero && currentTime.TotalMinutes <
            stationEvent.ReoccurrenceDelay + lastRun.TotalMinutes)
        {
            return false;
        }

        return true;
    }
}
