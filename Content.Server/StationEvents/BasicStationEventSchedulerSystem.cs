using System.Linq;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Configurations;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationEvents
{
    /// <summary>
    ///     The basic event scheduler rule, loosely based off of /tg/ events, which most
    ///     game presets use.
    /// </summary>
    [UsedImplicitly]
    public sealed class BasicStationEventSchedulerSystem : GameRuleSystem
    {
        public override string Prototype => "BasicStationEventScheduler";

        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;

        private const float MinimumTimeUntilFirstEvent = 300;
        private ISawmill _sawmill = default!;

        /// <summary>
        /// How long until the next check for an event runs
        /// </summary>
        /// Default value is how long until first event is allowed
        private float _timeUntilNextEvent = MinimumTimeUntilFirstEvent;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("basicevents");

            // Can't just check debug / release for a default given mappers need to use release mode
            // As such we'll always pause it by default.
            _configurationManager.OnValueChanged(CCVars.EventsEnabled, SetEnabled, true);

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _configurationManager.UnsubValueChanged(CCVars.EventsEnabled, SetEnabled);
        }

        public bool EventsEnabled { get; private set; }
        private void SetEnabled(bool value) => EventsEnabled = value;

        public override void Started() { }
        public override void Ended() { }

        /// <summary>
        /// Randomly run a valid event <b>immediately</b>, ignoring earlieststart or whether the event is enabled
        /// </summary>
        /// <returns></returns>
        public string RunRandomEvent()
        {
            var randomEvent = PickRandomEvent();

            if (randomEvent == null
                || !_prototype.TryIndex<GameRulePrototype>(randomEvent.Id, out var proto))
            {
                return Loc.GetString("station-event-system-run-random-event-no-valid-events");
            }

            GameTicker.AddGameRule(proto);
            return Loc.GetString("station-event-system-run-event",("eventName", randomEvent.Id));
        }

        /// <summary>
        /// Randomly picks a valid event.
        /// </summary>
        public StationEventRuleConfiguration? PickRandomEvent()
        {
            var availableEvents = AvailableEvents(true);
            return FindEvent(availableEvents);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!RuleStarted || !EventsEnabled)
                return;

            if (_timeUntilNextEvent > 0)
            {
                _timeUntilNextEvent -= frameTime;
                return;
            }

            // No point hammering this trying to find events if none are available
            var stationEvent = FindEvent(AvailableEvents());
            if (stationEvent == null
                || !_prototype.TryIndex<GameRulePrototype>(stationEvent.Id, out var proto))
            {
                return;
            }

            GameTicker.AddGameRule(proto);
            ResetTimer();
            _sawmill.Info($"Started event {proto.ID}. Next event in {_timeUntilNextEvent} seconds");
        }

        /// <summary>
        /// Reset the event timer once the event is done.
        /// </summary>
        private void ResetTimer()
        {
            // 5 - 15 minutes. TG does 3-10 but that's pretty frequent
            _timeUntilNextEvent = _random.Next(300, 900);
        }

        /// <summary>
        /// Pick a random event from the available events at this time, also considering their weightings.
        /// </summary>
        /// <returns></returns>
        private StationEventRuleConfiguration? FindEvent(List<StationEventRuleConfiguration> availableEvents)
        {
            if (availableEvents.Count == 0)
            {
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

        private void Reset(RoundRestartCleanupEvent ev)
        {
            _timeUntilNextEvent = MinimumTimeUntilFirstEvent;
        }
    }
}
