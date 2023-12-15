using System.Linq;
using Content.Server.Administration;
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
            // 5 - 25 minutes. TG does 3-10 but that's pretty frequent
            component.TimeUntilNextEvent = _random.Next(300, 1500);
        }
    }

    [ToolshedCommand, AdminCommand(AdminFlags.Debug)]
    public sealed class StationEventCommand : ToolshedCommand
    {
        private EventManagerSystem? _stationEvent;

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
