using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Random;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;

namespace Content.Server.StationEvents
{
    /// <summary>
    ///     The basic event scheduler rule, loosely based off of /tg/ events, which most
    ///     game presets use.
    /// </summary>
    [UsedImplicitly]
    public sealed class BasicStationEventSchedulerSystem : GameRuleSystem<BasicStationEventSchedulerComponent>
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly EventManagerSystem _event = default!;

        public const float MinEventTime = 60 * 3;
        public const float MaxEventTime = 60 * 10;

        protected override void Ended(EntityUid uid, BasicStationEventSchedulerComponent component, GameRuleComponent gameRule,
            GameRuleEndedEvent args)
        {
            component.TimeUntilNextEvent = BasicStationEventSchedulerComponent.MinimumTimeUntilFirstEvent;
        }


        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_event.EventsEnabled)
                return;

            var query = EntityQueryEnumerator<BasicStationEventSchedulerComponent, GameRuleComponent>();
            while (query.MoveNext(out var uid, out var eventScheduler, out var gameRule))
            {
                if (!GameTicker.IsGameRuleActive(uid, gameRule))
                    continue;

                if (eventScheduler.TimeUntilNextEvent > 0)
                {
                    eventScheduler.TimeUntilNextEvent -= frameTime;
                    return;
                }

                _event.RunRandomEvent();
                ResetTimer(eventScheduler);
            }
        }

        /// <summary>
        /// Reset the event timer once the event is done.
        /// </summary>
        private void ResetTimer(BasicStationEventSchedulerComponent component)
        {
            component.TimeUntilNextEvent = _random.NextFloat(MinEventTime, MaxEventTime);
        }
    }

    [ToolshedCommand, AdminCommand(AdminFlags.Debug)]
    public sealed class StationEventCommand : ToolshedCommand
    {
        private EventManagerSystem? _stationEvent;
        private BasicStationEventSchedulerSystem? _basicScheduler;
        private IRobustRandom? _random;

        /// <summary>
        ///     Estimates the expected number of times an event will run over the course of X rounds, taking into account weights and
        ///     how many events are expected to run over a given timeframe for a given playercount by repeatedly simulating rounds.
        ///     Effectively /100 (if you put 100 rounds) = probability an event will run per round.
        /// </summary>
        /// <remarks>
        ///     This isn't perfect. Code path eventually goes into <see cref="EventManagerSystem.CanRun"/>, which requires
        ///     state from <see cref="GameTicker"/>. As a result, you should probably just run this locally and not doing
        ///     a real round (it won't pollute the state, but it will get contaminated by previously ran events in the actual round)
        ///     and things like `MaxOccurrences` and `ReoccurrenceDelay` won't be respected.
        ///
        ///     I consider these to not be that relevant to the analysis here though (and I don't want most uses of them
        ///     to even exist) so I think it's fine.
        /// </remarks>
        [CommandImplementation("simulate")]
        public IEnumerable<(string, float)> Simulate([CommandArgument] int rounds, [CommandArgument] int playerCount, [CommandArgument] float roundEndMean, [CommandArgument] float roundEndStdDev)
        {
            _stationEvent ??= GetSys<EventManagerSystem>();
            _basicScheduler ??= GetSys<BasicStationEventSchedulerSystem>();
            _random ??= IoCManager.Resolve<IRobustRandom>();

            var occurrences = new Dictionary<string, int>();

            foreach (var ev in _stationEvent.AllEvents())
            {
                occurrences.Add(ev.Key.ID, 0);
            }

            for (var i = 0; i < rounds; i++)
            {
                var curTime = TimeSpan.Zero;
                var randomEndTime = _random.NextGaussian(roundEndMean, roundEndStdDev) * 60; // *60 = minutes to seconds
                if (randomEndTime <= 0)
                    continue;

                while (curTime.TotalSeconds < randomEndTime)
                {
                    // sim an event
                    curTime += TimeSpan.FromSeconds(_random.NextFloat(BasicStationEventSchedulerSystem.MinEventTime, BasicStationEventSchedulerSystem.MaxEventTime));
                    var available = _stationEvent.AvailableEvents(false, playerCount, curTime);
                    var ev = _stationEvent.FindEvent(available);
                    if (ev == null)
                        continue;

                    occurrences[ev] += 1;
                }
            }

            return occurrences.Select(p => (p.Key, (float) p.Value)).OrderByDescending(p => p.Item2);
        }

        [CommandImplementation("lsprob")]
        public IEnumerable<(string, float)> LsProb()
        {
            _stationEvent ??= GetSys<EventManagerSystem>();
            var events = _stationEvent.AllEvents();

            var totalWeight = events.Sum(x => x.Value.Weight);

            foreach (var (proto, comp) in events)
            {
                yield return (proto.ID, comp.Weight / totalWeight);
            }
        }

        [CommandImplementation("lsprobtime")]
        public IEnumerable<(string, float)> LsProbTime([CommandArgument] float time)
        {
            _stationEvent ??= GetSys<EventManagerSystem>();
            var events = _stationEvent.AllEvents().Where(pair => pair.Value.EarliestStart <= time).ToList();

            var totalWeight = events.Sum(x => x.Value.Weight);

            foreach (var (proto, comp) in events)
            {
                yield return (proto.ID, comp.Weight / totalWeight);
            }
        }

        [CommandImplementation("prob")]
        public float Prob([CommandArgument] string eventId)
        {
            _stationEvent ??= GetSys<EventManagerSystem>();
            var events = _stationEvent.AllEvents();

            var totalWeight = events.Sum(x => x.Value.Weight);
            var weight = 0f;
            if (events.TryFirstOrNull(p => p.Key.ID == eventId, out var pair))
            {
                weight = pair.Value.Value.Weight;
            }

            return weight / totalWeight;
        }
    }
}
