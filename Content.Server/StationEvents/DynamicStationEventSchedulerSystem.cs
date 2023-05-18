using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost.Components;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Metric;
using Content.Shared.chaos;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Prototypes;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.StationEvents;

public sealed class RankedEvent
{
    public PossibleEvent PossibleEvent;
    public ChaosMetrics Result;
    public FixedPoint2 Score;

    public RankedEvent(PossibleEvent possibleEvent, ChaosMetrics result, FixedPoint2 score)
    {
        PossibleEvent = possibleEvent;
        Result = result;
        Score = score;
    }
}

public sealed class PlayerCount
{
    public int Players;
    public int Ghosts;
}

/// <summary>
///     A scheduler which tries to keep station chaos within a set bound over time with the most suitable
///        good or bad events to nudge it in the correct direction.
/// </summary>
[UsedImplicitly]
public sealed class DynamicStationEventSchedulerSystem : GameRuleSystem<DynamicStationEventSchedulerComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EventManagerSystem _event = default!;
    [Dependency] private readonly StationMetricSystem _metrics = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] public readonly GameTicker _gameTicker = default!;

    protected override void Added(EntityUid uid, DynamicStationEventSchedulerComponent scheduler, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        // This deletes all existing metrics and sets them up again.
        _metrics.SetupMetrics();
        SetupEvents(scheduler, CountActivePlayers());
    }

    private void SetupEvents(DynamicStationEventSchedulerComponent scheduler, PlayerCount count)
    {
        scheduler.PossibleEvents.Clear();
        foreach (var proto in _prototypeManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract)
                continue;

            if (!proto.HasComponent<GameRuleComponent>(_factory))
                continue;

            if (!proto.TryGetComponent<StationEventComponent>(out var stationEvent, _factory))
                continue;

            if (!_event.CanRun(proto, stationEvent, count.Players, TimeSpan.MaxValue))
                continue;

            scheduler.PossibleEvents.Add(new PossibleEvent(proto.ID, stationEvent.Chaos));
        }
    }

    protected override void Ended(EntityUid uid, DynamicStationEventSchedulerComponent scheduler, GameRuleComponent gameRule,
        GameRuleEndedEvent args)
    {
        scheduler.TimeUntilNextEvent = BasicStationEventSchedulerComponent.MinimumTimeUntilFirstEvent;
    }

    protected override void ActiveTick(EntityUid uid, DynamicStationEventSchedulerComponent scheduler, GameRuleComponent gameRule, float frameTime)
    {
        scheduler.BeatTime += frameTime;
        if (scheduler.TimeUntilNextEvent > 0)
        {
            scheduler.TimeUntilNextEvent -= frameTime;
            return;
        }

        var chaos = _metrics.CalculateChaos();
        _adminLogger.Add(LogType.DynamicRule, LogImpact.Low, $"Station chaos is now {chaos.ToString()}");

        var count = CountActivePlayers();
        var beat = DetermineNextBeat(scheduler, chaos, count);
        var bestEvents = ChooseEvents(scheduler, beat, chaos, count);
        // Run the best event here
        if (bestEvents.Count > 0)
        {
            var chosenEvent = SelectBest(bestEvents, beat.RandomEventLimit);
            _event.RunNamedEvent(chosenEvent.PossibleEvent.PrototypeId);

            // Don't select this event again for the current story (when SetupEvents is called again)
            scheduler.PossibleEvents.Remove(chosenEvent.PossibleEvent);

            // 3 - 6 minutes until the next event is considered.
            scheduler.TimeUntilNextEvent = _random.NextFloat(120f, 360f);
        }
        else
        {
            // No events were run. Consider again in 30 seconds.
            scheduler.TimeUntilNextEvent = 30f;
        }
    }

    private PlayerCount CountActivePlayers()
    {
        var allPlayers = _playerManager.ServerSessions.ToList();
        var count = new PlayerCount();
        foreach (var player in allPlayers)
        {
            // TODO: A
            if (player.AttachedEntity != null)
            {
                if (HasComp<HumanoidAppearanceComponent>(player.AttachedEntity))
                {
                    count.Players += 1;
                }
                else if (HasComp<GhostComponent>(player.AttachedEntity))
                {
                    count.Ghosts += 1;
                }
            }
        }

        return count;
    }

    protected RankedEvent SelectBest(List<RankedEvent> bestEvents, int maxRandom)
    {
        var ranked =bestEvents.OrderBy(ev => ev.Score).Take(maxRandom).ToList();

        var events = String.Join(", ", ranked.Select(r => r.PossibleEvent.PrototypeId));
        _adminLogger.Add(LogType.DynamicRule, LogImpact.Low, $"Picked best events (in sequence) {events}");

        foreach (var rankedEvent in ranked)
        {
            // I'd like a nice weighted random here but I'm lazy
            if (_random.Prob(0.7f))
            {
                // Pick this event
                return rankedEvent;
            }
        }

        // Random dropped through all, just take best.
        return ranked[0];
    }

    private StoryBeat DetermineNextBeat(DynamicStationEventSchedulerComponent scheduler, ChaosMetrics chaos, PlayerCount count)
    {
        // Potentially Complete CurrBeat
        if (scheduler.CurrStory.Count > 0)
        {
            var beatName = scheduler.CurrStory[0];
            var beat = scheduler.StoryBeats[beatName];

            if (scheduler.BeatTime > beat.MaxSecs)
            {
                // Done with this beat (it's lasted too long)
                _adminLogger.Add(LogType.DynamicRule, LogImpact.Low, $"StoryBeat {beatName} complete. It's lasted {scheduler.BeatTime} out of a maximum of {beat.MaxSecs} seconds.");
            }
            else if (scheduler.BeatTime > beat.MinSecs)
            {
                // Determine if we meet the chaos thresholds to exit this beat
                if (!beat.EndIfAnyWorse.Empty && chaos.AnyWorseThan(beat.EndIfAnyWorse))
                {
                    // Done with this beat (chaos exceeded set bad level)
                    _adminLogger.Add(LogType.DynamicRule, LogImpact.Low, $"StoryBeat {beatName} complete. Chaos exceeds {beat.EndIfAnyWorse} (EndIfAnyWorse).");
                }
                else if(!beat.EndIfAllBetter.Empty && chaos.AllBetterThan(beat.EndIfAllBetter))
                {
                    // Done with this beat (chaos reached set good level)
                    _adminLogger.Add(LogType.DynamicRule, LogImpact.Low, $"StoryBeat {beatName} complete. Chaos better than {beat.EndIfAllBetter} (EndIfAllBetter).");
                }
                else
                {
                    return beat;
                }
            }
            else
            {
                return beat;
            }

            // If we didn't return by here, we are done with this beat.
            scheduler.CurrStory.RemoveAt(0);
        }

        scheduler.BeatTime = 0.0f;
        if (scheduler.CurrStory.Count > 0)
        {
            // Return the next beat in the current story.
            var beatName = scheduler.CurrStory[0];
            var beat = scheduler.StoryBeats[beatName];

            _adminLogger.Add(LogType.DynamicRule, LogImpact.Low, $"New StoryBeat {beatName}: {beat.Description}. Goal is {beat.Goal}");
            return beat;
        }

        // Need to find a new story
        var stories = scheduler.Stories.Keys.ToList();
        _random.Shuffle(stories);

        foreach (var storyName in stories)
        {
            var story = scheduler.Stories[storyName];
            if (story.MinPlayers > count.Players || story.MaxPlayers < count.Players)
            {
                continue;
            }

            scheduler.CurrStory = story.Beats.ShallowClone();
            scheduler.CurrStoryName = storyName;
            SetupEvents(scheduler, count);
            _adminLogger.Add(LogType.DynamicRule, LogImpact.Low, $"New Story {storyName}: {story.Description}. {scheduler.PossibleEvents.Count} events to use.");

            var beatName = scheduler.CurrStory[0];
            var beat = scheduler.StoryBeats[beatName];

            _adminLogger.Add(LogType.DynamicRule, LogImpact.Low, $"First StoryBeat {beatName}: {beat.Description}. Goal is {beat.Goal}");
            return beat;
        }

        // Just use the fallback beat (that does exist, right!?)
        scheduler.CurrStory.Add(scheduler.FallbackBeatName);
        return scheduler.StoryBeats[scheduler.FallbackBeatName];
    }

    private FixedPoint2 RankChaosDelta(ChaosMetrics chaos)
    {
        // Just a sum of squares (trying to get close to 0 on every score)
        //   Lower is better
        return chaos.ChaosDict.Values.Sum(v => (float)(v * v));
    }

    private List<RankedEvent> ChooseEvents(DynamicStationEventSchedulerComponent scheduler, StoryBeat beat, ChaosMetrics chaos, PlayerCount count)
    {
        // TODO : Potentially filter Chaos here using CriticalLevels & DangerLevels which force us to focus on
        //        big problems (lots of hostiles, spacing) prior to smaller ones (food & drink)
        var desiredChange = beat.Goal.ExclusiveSubtract(chaos);
        var result = FilterAndScore(scheduler, chaos, desiredChange, count);

        if (result.Count > 0)
        {
            return result;
        }

        // Fall back to improving all scores (not just the ones the beat is focused on)
        //   Generally this means reducing chaos (unspecified scores are desired to be 0).
        var allDesiredChange = beat.Goal - chaos;
        result = FilterAndScore(scheduler, chaos, allDesiredChange, count);

        return result;
    }

    // Filter only to events which improve the chaos score in alignment with desiredChange.
    //   Score them (lower is better) in how well they do this.
    private List<RankedEvent> FilterAndScore(DynamicStationEventSchedulerComponent scheduler, ChaosMetrics chaos,
        ChaosMetrics desiredChange, PlayerCount count)
    {
        var noEvent = RankChaosDelta(desiredChange);
        var result = new List<RankedEvent>();

        // Choose an event that specifically achieves chaos goals, focusing only on them.
        foreach (var possibleEvent in scheduler.PossibleEvents)
        {
            // How much of the relevant chaos will be left after this event has occurred
            var relevantChaosDelta = desiredChange.ExclusiveSubtract(possibleEvent.Chaos);
            var rank = RankChaosDelta(relevantChaosDelta);

            var allChaosAfter = chaos + possibleEvent.Chaos;

            if (rank < noEvent)
            {
                // Look up this event's prototype and check it is ready to run.
                var proto = _prototypeManager.Index<EntityPrototype>(possibleEvent.PrototypeId);

                if (!proto.TryGetComponent<StationEventComponent>(out var stationEvent, _factory))
                    continue;

                if (!_event.CanRun(proto, stationEvent, count.Players, _gameTicker.RoundDuration()))
                    continue;

                result.Add(new RankedEvent(possibleEvent, allChaosAfter, rank));
            }
        }

        return result;
    }
}
