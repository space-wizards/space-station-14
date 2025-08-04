using System.Linq;
using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.StationEvents.Components;
using Content.Shared.Administration;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.TypeParsers;
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

        protected override void Started(EntityUid uid, BasicStationEventSchedulerComponent component, GameRuleComponent gameRule,
            GameRuleStartedEvent args)
        {
            // A little starting variance so schedulers dont all proc at once.
            component.TimeUntilNextEvent = RobustRandom.NextFloat(component.MinimumTimeUntilFirstEvent, component.MinimumTimeUntilFirstEvent + 120);
        }

        protected override void Ended(EntityUid uid, BasicStationEventSchedulerComponent component, GameRuleComponent gameRule,
            GameRuleEndedEvent args)
        {
            component.TimeUntilNextEvent = component.MinimumTimeUntilFirstEvent;
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
                    continue;
                }

                _event.RunRandomEvent(eventScheduler.ScheduledGameRules);
                ResetTimer(eventScheduler);
            }
        }

        /// <summary>
        /// Reset the event timer once the event is done.
        /// </summary>
        private void ResetTimer(BasicStationEventSchedulerComponent component)
        {
            component.TimeUntilNextEvent = component.MinMaxEventTiming.Next(_random);
        }
    }

    [ToolshedCommand, AdminCommand(AdminFlags.Debug)]
    public sealed class StationEventCommand : ToolshedCommand
    {
        private EventManagerSystem? _stationEvent;
        private EntityTableSystem? _entityTable;
        private IComponentFactory? _compFac;
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
        public IEnumerable<(string, float)> Simulate([CommandArgument] Prototype<EntityPrototype> eventSchedulerProto, [CommandArgument] int rounds, [CommandArgument] int playerCount, [CommandArgument] float roundEndMean, [CommandArgument] float roundEndStdDev)
        {
            _stationEvent ??= GetSys<EventManagerSystem>();
            _entityTable ??= GetSys<EntityTableSystem>();
            _compFac ??= IoCManager.Resolve<IComponentFactory>();
            _random ??= IoCManager.Resolve<IRobustRandom>();

            var occurrences = new Dictionary<string, int>();

            foreach (var ev in _stationEvent.AllEvents())
            {
                occurrences.Add(ev.Key.ID, 0);
            }

            eventSchedulerProto.Deconstruct(out EntityPrototype eventScheduler);

            if (!eventScheduler.TryGetComponent<BasicStationEventSchedulerComponent>(out var basicScheduler, _compFac))
            {
                return occurrences.Select(p => (p.Key, (float)p.Value)).OrderByDescending(p => p.Item2);
            }

            var compMinMax = basicScheduler.MinMaxEventTiming; // we gotta do this since we cant execute on comp w/o an ent.

            for (var i = 0; i < rounds; i++)
            {
                var curTime = TimeSpan.Zero;
                var randomEndTime = _random.NextGaussian(roundEndMean, roundEndStdDev) * 60; // Its in minutes, should probably be a better time format once we get that in toolshed like [hh:mm:ss]
                if (randomEndTime <= 0)
                    continue;

                while (curTime.TotalSeconds < randomEndTime)
                {
                    // sim an event
                    curTime += TimeSpan.FromSeconds(compMinMax.Next(_random));

                    var available = _stationEvent.AvailableEvents(false, playerCount, curTime);
                    if (!_stationEvent.TryBuildLimitedEvents(basicScheduler.ScheduledGameRules, available, out var selectedEvents))
                    {
                        continue; // doesnt break because maybe the time is preventing events being available.
                    }

                    var ev = _stationEvent.FindEvent(selectedEvents);
                    if (ev == null)
                        continue;

                    occurrences[ev] += 1;
                }
            }

            return occurrences.Select(p => (p.Key, (float) p.Value)).OrderByDescending(p => p.Item2);
        }

        [CommandImplementation("lsprob")]
        public IEnumerable<(string, float)> LsProb([CommandArgument] Prototype<EntityPrototype> eventSchedulerProto)
        {
            _compFac ??= IoCManager.Resolve<IComponentFactory>();
            _stationEvent ??= GetSys<EventManagerSystem>();

            eventSchedulerProto.Deconstruct(out EntityPrototype eventScheduler);

            if (!eventScheduler.TryGetComponent<BasicStationEventSchedulerComponent>(out var basicScheduler, _compFac))
                yield break;

            var available = _stationEvent.AvailableEvents();
            if (!_stationEvent.TryBuildLimitedEvents(basicScheduler.ScheduledGameRules, available, out var events))
                yield break;

            var totalWeight = events.Sum(x => x.Value.Weight); // Well this shit definitely isnt correct now, and I see no way to make it correct.
                                                               // Its probably *fine* but it wont be accurate if the EntityTableSelector does any subsetting.
            foreach (var (proto, comp) in events)              // The only solution I see is to do a simulation, and we already have that, so...!
            {
                yield return (proto.ID, comp.Weight / totalWeight);
            }
        }

        [CommandImplementation("lsprobtheoretical")]
        public IEnumerable<(string, float)> LsProbTime([CommandArgument] Prototype<EntityPrototype> eventSchedulerProto, [CommandArgument] int playerCount, [CommandArgument] float time)
        {
            _compFac ??= IoCManager.Resolve<IComponentFactory>();
            _stationEvent ??= GetSys<EventManagerSystem>();

            eventSchedulerProto.Deconstruct(out EntityPrototype eventScheduler);

            if (!eventScheduler.TryGetComponent<BasicStationEventSchedulerComponent>(out var basicScheduler, _compFac))
                yield break;

            var timemins = time * 60;
            var theoryTime = TimeSpan.Zero + TimeSpan.FromSeconds(timemins);
            var available = _stationEvent.AvailableEvents(false, playerCount, theoryTime);
            if (!_stationEvent.TryBuildLimitedEvents(basicScheduler.ScheduledGameRules, available, out var untimedEvents))
                yield break;

            var events = untimedEvents.Where(pair => pair.Value.EarliestStart <= timemins).ToList();

            var totalWeight = events.Sum(x => x.Value.Weight); // same subsetting issue as lsprob.

            foreach (var (proto, comp) in events)
            {
                yield return (proto.ID, comp.Weight / totalWeight);
            }
        }

        [CommandImplementation("prob")]
        public float Prob([CommandArgument] Prototype<EntityPrototype> eventSchedulerProto, [CommandArgument] string eventId)
        {
            _compFac ??= IoCManager.Resolve<IComponentFactory>();
            _stationEvent ??= GetSys<EventManagerSystem>();

            eventSchedulerProto.Deconstruct(out EntityPrototype eventScheduler);

            if (!eventScheduler.TryGetComponent<BasicStationEventSchedulerComponent>(out var basicScheduler, _compFac))
                return 0f;

            var available = _stationEvent.AvailableEvents();
            if (!_stationEvent.TryBuildLimitedEvents(basicScheduler.ScheduledGameRules, available, out var events))
                return 0f;

            var totalWeight = events.Sum(x => x.Value.Weight); // same subsetting issue as lsprob.
            var weight = 0f;
            if (events.TryFirstOrNull(p => p.Key.ID == eventId, out var pair))
            {
                weight = pair.Value.Value.Weight;
            }

            return weight / totalWeight;
        }
    }
}
