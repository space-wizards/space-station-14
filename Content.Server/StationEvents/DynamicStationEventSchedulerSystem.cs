using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Ghost.Components;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Metric;
using Content.Shared.chaos;
using Content.Shared.Database;
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
    public EntityPrototype Prototype;
    public ChaosMetrics Chaos = new();
    public ChaosMetrics Result = new();
    public float Score = 0.0f;

    public RankedEvent(EntityPrototype prototype)
    {
        Prototype = prototype;
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
        if (scheduler.TimeUntilNextEvent > 0)
        {
            scheduler.TimeUntilNextEvent -= frameTime;
            return;
        }

        var chaos = _metrics.CalculateChaos();
        _adminLogger.Add(LogType.DynamicRule, LogImpact.Low, $"Station chaos is now {chaos.ToString()}");

        var beat = DetermineNextBeat(scheduler, chaos);
        var bestEvents = ChooseEvents(scheduler, beat, chaos);
        // Run the best event here

        ResetTimer(scheduler);
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


    private StoryBeat DetermineNextBeat(DynamicStationEventSchedulerComponent scheduler, ChaosMetrics chaos)
    {
        // Potentially Complete CurrBeat
        if (scheduler.CurrStory.Count > 0)
        {
            var beatName = scheduler.CurrStory[0];
            var beat = scheduler.StoryBeats[beatName];

            if (scheduler.BeatTime > beat.MaxSecs)
            {
                // Done with this beat (it's lasted too long)
            }
            else if (scheduler.BeatTime > beat.MinSecs)
            {
                // Determine if we meet the chaos thresholds to exit this beat
                if (!beat.EndIfAnyWorse.Empty && chaos.AnyWorseThan(beat.EndIfAnyWorse))
                {
                    // Done with this beat (chaos exceeded set bad level)
                }
                else if(!beat.EndIfAllBetter.Empty && chaos.AllBetterThan(beat.EndIfAllBetter))
                {
                    // Done with this beat (chaos reached set good level)
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

        if (scheduler.CurrStory.Count > 0)
        {
            // Return the next beat in the current story.
            var beatName = scheduler.CurrStory[0];
            return scheduler.StoryBeats[beatName];
        }

        // Need to find a new story
        var players = CountActivePlayers();
        var stories = scheduler.Stories.Keys.ToList();
        _random.Shuffle(stories);

        foreach (var storyName in stories)
        {
            var story = scheduler.Stories[storyName];
            if (story.MinPlayers > players.Players || story.MaxPlayers < players.Players)
            {
                continue;
            }

            scheduler.CurrStory = story.Beats.ShallowClone();
            scheduler.CurrStoryName = storyName;
            SetupEvents(scheduler, players);

            var beatName = scheduler.CurrStory[0];
            return scheduler.StoryBeats[beatName];
        }

        // Just use the fallback beat (that does exist, right!?)
        scheduler.CurrStory.Add(scheduler.FallbackBeatName);
        return scheduler.StoryBeats[scheduler.FallbackBeatName];
    }

    private List<RankedEvent> ChooseEvents(DynamicStationEventSchedulerComponent scheduler, StoryBeat beat, ChaosMetrics chaos)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Reset the event timer once the event is done.
    /// </summary>
    private void ResetTimer(DynamicStationEventSchedulerComponent scheduler)
    {
        // For testing, 30 secs.
        scheduler.TimeUntilNextEvent = 30f;

        // // 4 - 12 minutes.
        // scheduler.TimeUntilNextEvent = _random.NextFloat(240f, 720f);
    }
}
