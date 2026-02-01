using System.Linq;
using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Server.StationEvents.Components;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.EntityTable;

namespace Content.Server.StationEvents;

public sealed class EventManagerSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] public readonly GameTicker GameTicker = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    public bool EventsEnabled { get; private set; }
    private void SetEnabled(bool value) => EventsEnabled = value;

    public Dictionary<EntityPrototype, StationEventComponent>? AllEventCache;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        Subs.CVar(_configurationManager, CCVars.EventsEnabled, SetEnabled, true);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            AllEventCache = GetAllEvents();
    }

    /// <summary>
    /// Randomly runs an event from provided EntityTableSelector.
    /// </summary>
    public void RunRandomEvent(EntityTableSelector limitedEventsTable)
    {
        if (!TryBuildLimitedEvents(limitedEventsTable, out var limitedEvents))
        {
            Log.Warning("Provided event table could not build dict!");
            return;
        }

        // This picks the event. Arguably we should be doing this with GetSpawns but that would be a massive amount of YAML slop.
        // Or you'd need a new table prototype which inherits from EntityTables with its own logic for events.
        // It's a ton of effort that only results in Events being able to use GroupSelectors so not worth it unless you're insane.
        if (FindEvent(limitedEvents) is not { } randomLimitedEvent)
        {
            Log.Warning("The selected random event is null!");
            return;
        }

        if (!_prototype.Resolve(randomLimitedEvent, out _))
        {
            Log.Warning("A requested event is not available!");
            return;
        }

        GameTicker.AddGameRule(randomLimitedEvent);
    }

    /// <summary>
    /// Builds a list of all possible events and their probabilities.
    /// </summary>
    public IEnumerable<(EntProtoId, double)> ListLimitedEvents(
        EntityTableSelector limitedEventsTable,
        TimeSpan? currentTime = null,
        int? playerCount = null)
    {
        var selectedEvents = _entityTable.ListSpawns(limitedEventsTable);

        return ListLimitedEvents(selectedEvents, currentTime, playerCount);
    }

    /// <inheritdoc cref="TryBuildLimitedEvents(IEnumerable{EntProtoId},out Dictionary{EntityPrototype,StationEventComponent},TimeSpan?,int?)"/>
    public bool TryBuildLimitedEvents(
        EntityTableSelector limitedEventsTable,
        out Dictionary<EntityPrototype, StationEventComponent> limitedEvents,
        TimeSpan? currentTime = null,
        int? playerCount = null)
    {
        var selectedEvents = _entityTable.GetSpawns(limitedEventsTable);

        return TryBuildLimitedEvents(selectedEvents, out limitedEvents, currentTime, playerCount);
    }

    public IEnumerable<(EntProtoId, double)> ListLimitedEvents(
        IEnumerable<(EntProtoId, double)> selectedEvents,
        TimeSpan? currentTime = null,
        int? playerCount = null)
    {
        var limitedEvents = new List<(EntProtoId, double)>();

        playerCount ??= _playerManager.PlayerCount;

        // playerCount does a lock so we'll just keep the variable here
        currentTime ??= GameTicker.RoundDuration();

        var totalWeight = 0f;

        foreach (var (eventId, prob) in selectedEvents)
        {
            if (!_prototype.Resolve(eventId, out var eventproto))
                continue;

            if (eventproto.Abstract)
                continue;

            if (!eventproto.TryGetComponent<StationEventComponent>(out var stationEvent, EntityManager.ComponentFactory))
                continue;

            if (!CanRun(eventproto, stationEvent, playerCount.Value, currentTime.Value))
                continue;

            limitedEvents.Add((eventproto, prob * stationEvent.Weight));
            totalWeight += stationEvent.Weight;
        }

        if (!limitedEvents.Any() || totalWeight <= 0)
            yield break;

        for (var i = 0; i < limitedEvents.Count; i++)
        {
            var eventWeight = limitedEvents[i];
            eventWeight.Item2 /= totalWeight;
            yield return eventWeight;
        }
    }

    /// <summary>
    /// Builds a dictionary of valid event prototypes from a list of <see cref="EntProtoId"/>.
    /// Dictionary output consists of the valid prototype as the key, and the <see cref="StationEventComponent"/> as the value.
    /// </summary>
    /// <param name="selectedEvents">List of events we're selecting from.</param>
    /// <param name="limitedEvents">Dictionary we're outputting.</param>
    /// <param name="currentTime">Optional override for station time.</param>
    /// <param name="playerCount">Optional override for playerCount.</param>
    /// <returns>Returns true if the provided EntProtoId list has at least one prototype with a StationEventComp that can successfully run!</returns>
    public bool TryBuildLimitedEvents(
        IEnumerable<EntProtoId> selectedEvents,
        out Dictionary<EntityPrototype, StationEventComponent> limitedEvents,
        TimeSpan? currentTime = null,
        int? playerCount = null)
    {
        limitedEvents = new Dictionary<EntityPrototype, StationEventComponent>();

        playerCount ??= _playerManager.PlayerCount;

        // playerCount does a lock so we'll just keep the variable here
        currentTime ??= GameTicker.RoundDuration();

        foreach (var eventid in selectedEvents)
        {
            if (!_prototype.Resolve(eventid, out var eventproto))
            {
                Log.Warning("An event ID has no prototype index!");
                continue;
            }

            if (limitedEvents.ContainsKey(eventproto)) // This stops it from dying if you add duplicate entries in a fucked table
                continue;

            if (eventproto.Abstract)
                continue;

            if (!eventproto.TryGetComponent<StationEventComponent>(out var stationEvent, EntityManager.ComponentFactory))
                continue;

            if (!CanRun(eventproto, stationEvent, playerCount.Value, currentTime.Value))
                continue;

            limitedEvents.Add(eventproto, stationEvent);
        }

        if (!limitedEvents.Any())
            return false;

        return true;
    }

    /// <summary>
    /// Randomly picks a valid event.
    /// </summary>
    public string? PickRandomEvent()
    {
        var availableEvents = AvailableEvents();
        Log.Info($"Picking from {availableEvents.Count} total available events");
        return FindEvent(availableEvents);
    }

    /// <summary>
    /// Pick a random event from the available events at this time, also considering their weightings.
    /// </summary>
    /// <returns></returns>
    public string? FindEvent(Dictionary<EntityPrototype, StationEventComponent> availableEvents)
    {
        if (availableEvents.Count == 0)
        {
            Log.Warning("No events were available to run!");
            return null;
        }

        var sumOfWeights = 0.0f;

        foreach (var stationEvent in availableEvents.Values)
        {
            sumOfWeights += stationEvent.Weight;
        }

        sumOfWeights = _random.NextFloat(sumOfWeights);

        foreach (var (proto, stationEvent) in availableEvents)
        {
            sumOfWeights -= stationEvent.Weight;

            if (sumOfWeights <= 0.0f)
            {
                return proto.ID;
            }
        }

        Log.Error("Event was not found after weighted pick process!");
        return null;
    }

    /// <summary>
    /// Gets the events that have met their player count, time-until start, etc.
    /// </summary>
    /// <param name="playerCountOverride">Override for player count, if using this to simulate events rather than in an actual round.</param>
    /// <param name="currentTimeOverride">Override for round time, if using this to simulate events rather than in an actual round.</param>
    /// <returns></returns>
    public Dictionary<EntityPrototype, StationEventComponent> AvailableEvents(
        int? playerCountOverride = null,
        TimeSpan? currentTimeOverride = null)
    {
        var playerCount = playerCountOverride ?? _playerManager.PlayerCount;

        // playerCount does a lock so we'll just keep the variable here
        var currentTime = currentTimeOverride ?? GameTicker.RoundDuration();

        var result = new Dictionary<EntityPrototype, StationEventComponent>();

        foreach (var (proto, stationEvent) in AllEvents())
        {
            if (CanRun(proto, stationEvent, playerCount, currentTime))
            {
                result.Add(proto, stationEvent);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns all events prototypes which exist. Prioritizes the cache.
    /// </summary>
    /// <returns>All event prototypes, and their event component.</returns>
    public Dictionary<EntityPrototype, StationEventComponent> AllEvents()
    {
        return AllEventCache ?? GetAllEvents();
    }

    /// <summary>
    /// Gets all event prototypes that exist. Private because you should be using the cache!
    /// </summary>
    private Dictionary<EntityPrototype, StationEventComponent> GetAllEvents()
    {
        var allEvents = new Dictionary<EntityPrototype, StationEventComponent>();
        foreach (var prototype in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.Abstract)
                continue;

            if (!prototype.TryGetComponent<StationEventComponent>(out var stationEvent, EntityManager.ComponentFactory))
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

        if (_roundEnd.IsRoundEndRequested() && !stationEvent.OccursDuringRoundEnd && !_roundEnd.CanCallOrRecall())
        {
            return false;
        }

        return true;
    }
}
