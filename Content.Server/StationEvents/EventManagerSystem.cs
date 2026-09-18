using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Server.StationEvents.Components;
using Content.Shared.CCVar;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.GameTicking.Components;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationEvents;

public sealed partial class EventManagerSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _configurationManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private ServerGameTicker _gameTicker = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;

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
        if (FindEvent(limitedEvents.Value) is not { } randomLimitedEvent)
        {
            Log.Warning("The selected random event is null!");
            return;
        }

        if (!ProtoMan.Resolve(randomLimitedEvent, out _))
        {
            Log.Warning("A requested event is not available!");
            return;
        }

        _gameTicker.AddGameRule(randomLimitedEvent);
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

    /// <inheritdoc cref="TryBuildLimitedEvents(IEnumerable{EntProtoId},out EventTable?,TimeSpan?,int?)"/>
    public bool TryBuildLimitedEvents(
        EntityTableSelector limitedEventsTable,
        [NotNullWhen(true)] out EventTable? limitedEvents,
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
        currentTime ??= _gameTicker.RoundDuration();

        var totalWeight = 0d;

        foreach (var (eventId, prob) in selectedEvents)
        {
            if (!ProtoMan.Resolve(eventId, out var eventproto))
                continue;

            if (eventproto.Abstract)
                continue;

            if (!eventproto.TryComp<StationEventComponent>(out var stationEvent, EntityManager.ComponentFactory))
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
        [NotNullWhen(true)] out EventTable? limitedEvents,
        TimeSpan? currentTime = null,
        int? playerCount = null)
    {
        var events = new List<(EntityPrototype, float)>();
        var weight = 0d;

        playerCount ??= _playerManager.PlayerCount;

        // playerCount does a lock so we'll just keep the variable here
        currentTime ??= _gameTicker.RoundDuration();

        foreach (var eventid in selectedEvents)
        {
            if (_gameTicker.IsIgnored(eventid))
                continue;

            if (!ProtoMan.Resolve(eventid, out var eventproto))
            {
                Log.Warning("An event ID has no prototype index!");
                continue;
            }

            if (eventproto.Abstract)
                continue;

            if (!eventproto.TryComp<StationEventComponent>(out var stationEvent, EntityManager.ComponentFactory))
                continue;

            if (!CanRun(eventproto, stationEvent, playerCount.Value, currentTime.Value))
                continue;

            events.Add((eventproto, stationEvent.Weight));
            weight += stationEvent.Weight;
        }

        if (!events.Any())
        {
            limitedEvents = null;
            return false;
        }

        limitedEvents = new EventTable(events.AsReadOnly(), weight);
        return true;
    }

    /// <summary>
    /// Randomly picks a valid event.
    /// </summary>
    public string? PickRandomEvent()
    {
        var availableEvents = AvailableEvents();
        Log.Info($"Picking from {availableEvents.Events.Count()} total available events");
        return FindEvent(availableEvents);
    }

    /// <summary>
    /// Pick a random event from the available events at this time, also considering their weightings.
    /// </summary>
    /// <returns></returns>
    // TODO: Make this private :V
    public string? FindEvent(EventTable events)
    {
        if (events.Weight <= 0)
        {
            Log.Warning("No events were available to run!");
            return null;
        }

        var target = _random.NextDouble(0, events.Weight);

        foreach (var (proto, weight) in events.Events)
        {
            target -= weight;

            if (target <= 0)
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
    public EventTable AvailableEvents(
        int? playerCountOverride = null,
        TimeSpan? currentTimeOverride = null)
    {
        var playerCount = playerCountOverride ?? _playerManager.PlayerCount;

        // playerCount does a lock so we'll just keep the variable here
        var currentTime = currentTimeOverride ?? _gameTicker.RoundDuration();

        var result = new List<(EntityPrototype, float)>();
        var weight = 0d;

        foreach (var (proto, stationEvent) in AllEvents())
        {
            if (CanRun(proto, stationEvent, playerCount, currentTime))
            {
                weight += stationEvent.Weight;
                result.Add((proto, stationEvent.Weight));
            }
        }

        return new EventTable(result.AsReadOnly(), weight);
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
        foreach (var prototype in ProtoMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.Abstract)
                continue;

            if (!prototype.TryComp<StationEventComponent>(out var stationEvent, EntityManager.ComponentFactory))
                continue;

            allEvents.Add(prototype, stationEvent);
        }

        return allEvents;
    }

    private bool CanRun(EntityPrototype prototype, StationEventComponent stationEvent, int playerCount, TimeSpan currentTime)
    {
        // Do the really simple comparisons BEFORE we create an IEnumerable for GameRules :V
        if (playerCount < stationEvent.MinimumPlayers)
            return false;

        if (currentTime != TimeSpan.Zero && currentTime < TimeSpan.FromMinutes(stationEvent.EarliestStart))
            return false;

        // Slightly slower if we don't care about MaxOccurrences, but that's not a huge issue in the context of the event scheduler.
        var count = 0;
        var lastRun = TimeSpan.Zero;
        var ruleQuery = EntityQueryEnumerator<GameRuleComponent, MetaDataComponent>();
        while (ruleQuery.MoveNext(out var rule, out var meta))
        {
            if (meta.EntityPrototype?.ID != prototype.ID)
                continue;

            count++;
            if (lastRun < rule.ActivatedAt)
                lastRun = rule.ActivatedAt;
        }

        if (stationEvent.MaxOccurrences is { } maxOccurrences && count >= maxOccurrences)
            return false;

        if (count > 0 && currentTime < TimeSpan.FromMinutes(stationEvent.ReoccurrenceDelay) + lastRun)
            return false;

        return !_roundEnd.IsRoundEndRequested() || stationEvent.OccursDuringRoundEnd || _roundEnd.CanCallOrRecall();
    }
}

/// <summary>
/// A simple struct which stores a table of events with given weights, and the total weight of the events.
/// It's recommended to randomize this table before use.
/// </summary>
/// <param name="events">Array of events this table has, along with their weights.</param>
/// <param name="weight">Total Weight of the events.</param>
public readonly struct EventTable(ReadOnlyCollection<(EntityPrototype, float)> events, double weight)
{
    public readonly ReadOnlyCollection<(EntityPrototype, float)> Events = events;

    public readonly double Weight = weight;
}
