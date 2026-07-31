// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Server.StationEvents.Components;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationEvents;

public sealed class SurvivalRampingStationEventSchedulerSystem : GameRuleSystem<SurvivalRampingStationEventSchedulerComponent>
{
    private const int EventPickAttempts = 20;
    private const float FailedEventRetryCooldown = 30f;

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EventManagerSystem _event = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;

    public float GetChaosModifier(SurvivalRampingStationEventSchedulerComponent component)
    {
        var roundTime = (float) _gameTicker.RoundDuration().TotalSeconds;
        if (component.EndTime <= 0f)
            return component.MaxChaos;

        if (roundTime <= component.EndTime)
            return component.StartingChaos + (component.MaxChaos - component.StartingChaos) / component.EndTime * roundTime;

        return component.MaxChaos;
    }

    protected override void Started(EntityUid uid,
        SurvivalRampingStationEventSchedulerComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        component.MaxChaos = component.AverageChaos;
        component.EndTime = component.AverageEndTime * 60f;

        if (component.StartingChaos > component.MaxChaos)
            component.StartingChaos = component.MaxChaos;

        PickNextEventTime(component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTicker.RunLevel == GameRunLevel.PostRound)
            return;

        if (!_event.EventsEnabled)
            return;

        var query = EntityQueryEnumerator<SurvivalRampingStationEventSchedulerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var scheduler, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            TryPlayAlert(scheduler);

            if (scheduler.TimeUntilNextEvent > 0f)
            {
                scheduler.TimeUntilNextEvent -= frameTime;
                continue;
            }

            var phase = PickPhase(scheduler);
            if (phase == null)
            {
                Log.Warning("Survival event scheduler has no active phase.");
                continue;
            }

            // Do not reset the cooldown until an event was actually queued.
            // GroupSelector may pick a sub-pool that has no queueable events after
            // max occurrence / active-rule filtering, so retry a few times.
            if (!TryRunRandomEvent(scheduler, phase))
            {
                scheduler.TimeUntilNextEvent = FailedEventRetryCooldown;
                continue;
            }

            PickNextEventTime(scheduler);
        }
    }

    private void PickNextEventTime(SurvivalRampingStationEventSchedulerComponent component)
    {
        var mod = GetChaosModifier(component);

        component.TimeUntilNextEvent = _random.NextFloat(240f / mod, 720f / mod);
    }

    private bool TryRunRandomEvent(
        SurvivalRampingStationEventSchedulerComponent component,
        SurvivalRampingStationEventSchedulerPhase phase)
    {
        for (var i = 0; i < EventPickAttempts; i++)
        {
            if (!TryBuildSurvivalEvents(component, phase, out var limitedEvents))
                continue;

            if (_event.FindEvent(limitedEvents) is not { } randomEvent)
                continue;

            _gameTicker.AddGameRule(randomEvent);
            return true;
        }

        return false;
    }

    private bool TryBuildSurvivalEvents(
        SurvivalRampingStationEventSchedulerComponent component,
        SurvivalRampingStationEventSchedulerPhase phase,
        out Dictionary<EntityPrototype, StationEventComponent> limitedEvents)
    {
        limitedEvents = new Dictionary<EntityPrototype, StationEventComponent>();

        foreach (var eventId in _entityTable.GetSpawns(phase.ScheduledGameRules))
        {
            if (!_prototype.Resolve(eventId, out var eventPrototype))
            {
                Log.Warning($"Survival event ID {eventId} has no prototype index.");
                continue;
            }

            if (limitedEvents.ContainsKey(eventPrototype))
                continue;

            if (eventPrototype.Abstract)
                continue;

            if (!eventPrototype.TryGetComponent<StationEventComponent>(out var stationEvent, EntityManager.ComponentFactory))
                continue;

            if (!CanQueueSurvivalEvent(component, eventPrototype, stationEvent))
                continue;

            limitedEvents.Add(eventPrototype, stationEvent);
        }

        return limitedEvents.Count > 0;
    }

    private bool CanQueueSurvivalEvent(
        SurvivalRampingStationEventSchedulerComponent component,
        EntityPrototype prototype,
        StationEventComponent stationEvent)
    {
        // Survival phase tables are the local timing gate, so ignore only the
        // StationEvent time gates: EarliestStart and ReoccurrenceDelay.
        if (_gameTicker.IsGameRuleActive(prototype.ID))
            return false;

        if (stationEvent.WillNotStartRandomly)
            return false;

        var maxOccurrences = stationEvent.MaxOccurrences;
        if (component.MaxEventOccurrences.TryGetValue(prototype.ID, out var survivalMaxOccurrences) &&
            (!maxOccurrences.HasValue || survivalMaxOccurrences < maxOccurrences.Value))
        {
            maxOccurrences = survivalMaxOccurrences;
        }

        if (maxOccurrences.HasValue && GetOccurrences(prototype.ID) >= maxOccurrences.Value)
            return false;

        if (_player.PlayerCount < stationEvent.MinimumPlayers)
            return false;

        if (_roundEnd.IsRoundEndRequested() && !stationEvent.OccursDuringRoundEnd)
            return false;

        return true;
    }

    private int GetOccurrences(string stationEvent)
    {
        return _gameTicker.AllPreviousGameRules.Count(rule => rule.Item2 == stationEvent);
    }

    private SurvivalRampingStationEventSchedulerPhase? PickPhase(SurvivalRampingStationEventSchedulerComponent component)
    {
        var roundMinutes = (float) _gameTicker.RoundDuration().TotalMinutes;
        SurvivalRampingStationEventSchedulerPhase? selected = null;

        foreach (var phase in component.Phases)
        {
            if (phase.StartTime > roundMinutes)
                continue;

            if (selected == null || phase.StartTime > selected.StartTime)
                selected = phase;
        }

        return selected;
    }

    private void TryPlayAlert(SurvivalRampingStationEventSchedulerComponent component)
    {
        if (component.AlertPlayed || component.AlertTime is not { } alertTime)
            return;

        if (_gameTicker.RoundDuration().TotalMinutes < alertTime)
            return;

        if (component.AlertAnnouncement is { } announcement)
        {
            _chat.DispatchGlobalAnnouncement(
                Loc.GetString(announcement),
                component.AlertSender is { } sender ? Loc.GetString(sender) : null,
                playSound: component.AlertSound != null,
                announcementSound: component.AlertSound,
                colorOverride: component.AlertAnnouncementColor);
        }

        component.AlertPlayed = true;
    }
}
